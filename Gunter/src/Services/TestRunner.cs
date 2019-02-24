using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Forms;
using Gunter.Data;
using Gunter.Extensions;
using JetBrains.Annotations;
using Reusable;
using Reusable.Commander;
using Reusable.Exceptionizer;
using Reusable.IOnymous;
using Reusable.OmniLog;
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
        private readonly ICommandLineExecutor _commandLineExecutor;
        private readonly RuntimeVariableDictionaryFactory _runtimeVariableDictionaryFactory;

        public TestRunner
        (
            ILogger<TestRunner> logger,
            ICommandLineExecutor commandLineExecutor,
            RuntimeVariableDictionaryFactory runtimeVariableDictionaryFactory
        )
        {
            _logger = logger;
            _commandLineExecutor = commandLineExecutor;
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

            using (_logger.BeginScope().WithCorrelationHandle("TestBundle").AttachElapsed())
            using (Disposable.Create(() =>
            {
                foreach (var item in cache.Values) item.Dispose();
            }))
            {
                _logger.Log(Abstraction.Layer.Infrastructure().Meta(new { TestBundleFileName = testBundle.FileName }));
                foreach (var current in tests)
                {
                    using (_logger.BeginScope().WithCorrelationHandle("TestCase").AttachElapsed())
                    {
                        _logger.Log(Abstraction.Layer.Infrastructure().Meta(new { TestCaseId = current.testCase.Id }));
                        try
                        {
                            if (!cache.TryGetValue(current.dataSource.Id, out var cacheItem))
                            {
                                cache[current.dataSource.Id] = cacheItem = await current.dataSource.GetDataAsync(testBundle.DirectoryName, testBundleRuntimeVariables);
                            }

                            var (result, runElapsed, then) = RunTest(current.testCase, cacheItem.Value);

                            var context = new TestContext
                            {
                                TestBundle = testBundle,
                                TestCase = current.testCase,
                                //TestWhen = when,
                                DataSource = current.dataSource,
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
                                            GetDataElapsed = cacheItem.ElapsedQuery,
                                            RunTestElapsed = runElapsed
                                        },
                                    },
                                    testBundle.Variables.Flatten()
                                )
                            };

                            foreach (var cmd in then)
                            {
                                await _commandLineExecutor.ExecuteAsync(cmd, context);
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

        private (TestResult Result, TimeSpan Elapsed, IList<string> Then) RunTest(TestCase testCase, DataTable data)
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

            _logger.Log(Abstraction.Layer.Infrastructure().Meta(new { TestResult = testResult }));

            return
                testCase.When.TryGetValue(testResult, out var then)
                    ? (testResult, assertElapsed, then)
                    : (testResult, assertElapsed, default);
        }
    }
}