using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Gunter.Data;
using Gunter.Extensions;
using JetBrains.Annotations;
using Reusable;
using Reusable.Commander;
using Reusable.Exceptionize;
using Reusable.IOnymous;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter.Services
{
    public interface ITestRunner
    {
        Task RunAsync
        (
            [NotNull, ItemNotNull] IEnumerable<TestBundle> testBundles
        );
    }

    [UsedImplicitly]
    internal class TestRunner : ITestRunner
    {
        private readonly ILogger _logger;
        private readonly ICommandExecutor _commandLineExecutor;
        private readonly ICommandFactory _commandFactory;
        private readonly RuntimeVariableDictionaryFactory _runtimeVariableDictionaryFactory;

        public TestRunner
        (
            ILogger<TestRunner> logger,
            ICommandExecutor commandLineExecutor,
            ICommandFactory commandFactory,
            RuntimeVariableDictionaryFactory runtimeVariableDictionaryFactory
        )
        {
            _logger = logger;
            _commandLineExecutor = commandLineExecutor;
            _commandFactory = commandFactory;
            _runtimeVariableDictionaryFactory = runtimeVariableDictionaryFactory;
        }

        public async Task RunAsync(IEnumerable<TestBundle> testBundles)
        {
            var actions = new ActionBlock<TestBundle>
            (
                RunAsync,
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount * 2
                }
            );

            foreach (var testBundle in testBundles)
            {
                await actions.SendAsync(testBundle);
            }

            actions.Complete();
            await actions.Completion;
        }

        private async Task RunAsync(TestBundle testBundle)
        {
            var tests =
                from testCase in testBundle.Tests
                from dataSource in testCase.DataSources(testBundle)
                select (testCase, dataSource);

            var testBundleRuntimeVariables = _runtimeVariableDictionaryFactory.Create(new object[] { testBundle }, testBundle.Variables.Flatten());

            var cache = new Dictionary<SoftString, GetDataResult>();

            using (_logger.BeginScope().CorrelationHandle("TestBundle").AttachElapsed())
            using (Disposable.Create(() =>
            {
                foreach (var item in cache.Values) item.Dispose();
            }))
            {
                _logger.Log(Abstraction.Layer.Service().Meta(new { TestBundleFileName = testBundle.FileName }));
                foreach (var current in tests)
                {
                    using (_logger.BeginScope().CorrelationHandle("TestCase").AttachElapsed())
                    {
                        _logger.Log(Abstraction.Layer.Service().Meta(new { TestCaseId = current.testCase.Id }));
                        try
                        {
                            if (!cache.TryGetValue(current.dataSource.Id, out var cacheItem))
                            {
                                cache[current.dataSource.Id] = cacheItem = await current.dataSource.GetDataAsync(testBundleRuntimeVariables);
                            }

                            var (result, runElapsed, commands) = RunTest(current.testCase, cacheItem.Value);

                            var context = new TestContext
                            {
                                TestBundle = testBundle,
                                TestCase = current.testCase,
                                //TestWhen = when,
                                Log = current.dataSource,
                                Query = cacheItem.Query,
                                Data = cacheItem.Value,
                                Result = result,
                                RuntimeVariables = _runtimeVariableDictionaryFactory.Create
                                (
                                    new object[]
                                    {
                                        testBundle,
                                        current.testCase,
                                        //current.dataSource, // todo - not used - should be query
                                        new TestCounter
                                        {
                                            GetDataElapsed = cacheItem.GetDataElapsed,
                                            RunTestElapsed = runElapsed
                                        },
                                    },
                                    testBundle.Variables.Flatten()
                                )
                            };

                            foreach (var cmd in commands)
                            {
                                await _commandLineExecutor.ExecuteAsync(cmd, context, _commandFactory);
                            }

                            _logger.Log(Abstraction.Layer.Business().Routine(nameof(RunAsync)).Completed());
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (DynamicException ex) when (ex.NameMatches("^DataSource"))
                        {
                            _logger.Log(Abstraction.Layer.Business().Routine(nameof(RunAsync)).Faulted(), ex);
                            // It'd be pointless to continue when there is no data.
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.Log(Abstraction.Layer.Business().Routine(nameof(RunAsync)).Faulted(), ex);
                        }
                    }
                }
            }
        }

        private (TestResult Result, TimeSpan Elapsed, IList<string> Commands) RunTest(TestCase testCase, DataTable data)
        {
            var assertStopwatch = Stopwatch.StartNew();
            if (!(data.Compute(testCase.Assert, testCase.Filter) is bool result))
            {
                throw DynamicException.Create("Assert", $"'{nameof(TestCase.Assert)}' must evaluate to '{nameof(Boolean)}'.");
            }

            var assertElapsed = assertStopwatch.Elapsed;
            var testResult =
                result
                    ? TestResult.Passed
                    : TestResult.Failed;

            _logger.Log(Abstraction.Layer.Service().Meta(new { TestResult = testResult }));

            return
                testCase.When.TryGetValue(testResult, out var then)
                    ? (testResult, assertElapsed, then)
                    : (testResult, assertElapsed, new string[0]);
        }
    }
}