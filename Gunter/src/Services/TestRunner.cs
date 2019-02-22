using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Gunter.Data;
using Gunter.Extensions;
using JetBrains.Annotations;
using Reusable;
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
        private readonly RuntimeVariableDictionaryFactory _runtimeVariableDictionaryFactory;
        private readonly ILogger _logger;

        public TestRunner
        (
            ILogger<TestRunner> logger,
            IResourceProvider resourceProvider,
            RuntimeVariableDictionaryFactory runtimeVariableDictionaryFactory
        )
        {
            _runtimeVariableDictionaryFactory = runtimeVariableDictionaryFactory;
            _logger = logger;
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
            var testIndex = 0;
            var tests =
                from testCase in testBundle.Tests
                from dataSource in testCase.DataSources(testBundle)
                select (testCase, dataSource, testIndex: testIndex++);

            var runtimeVariables = _runtimeVariableDictionaryFactory.Create(new object[] { testBundle }, testBundle.Variables.Flatten());

            var cache = new Dictionary<SoftString, (DataTable Data, string Query, TimeSpan Elapsed)>();

            using (_logger.BeginScope().WithCorrelationHandle("TestBundle").AttachElapsed())
            using (Disposable.Create(() =>
            {
                foreach (var (data, _, _) in cache.Values)
                {
                    data.Dispose();
                }
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
                                var getDataStopwatch = Stopwatch.StartNew();
                                var (data, query) = await current.dataSource.GetDataAsync(testBundle.DirectoryName, runtimeVariables);
                                cache[current.dataSource.Id] = cacheItem = (data, query, getDataStopwatch.Elapsed);
                            }

                            var assertStopwatch = Stopwatch.StartNew();
                            var (result, actions) = RunTest(current.testCase, cacheItem.Data);
                            var assertElapsed = assertStopwatch.Elapsed;

                            if (actions.Alert())
                            {
                                var testCaseFormatter =
                                    _runtimeVariableDictionaryFactory.Create
                                    (
                                        new object[]
                                        {
                                            testBundle,
                                            current.testCase,
                                            current.dataSource, // todo - not used - should be query
                                            new TestCounter
                                            {
                                                GetDataElapsed = cacheItem.Elapsed,
                                                RunTestElapsed = assertElapsed
                                            },
                                        },
                                        testBundle.Variables.Flatten()
                                    );

                                await AlertAsync(new TestContext
                                {
                                    TestBundle = testBundle,
                                    TestCase = current.testCase,
                                    DataSource = current.dataSource,
                                    Data = cacheItem.Data,
                                    RuntimeVariables = testCaseFormatter,
                                    Query = cacheItem.Query,
                                    Result = result
                                });
                            }

                            if (actions.Halt())
                            {
                                break;
                            }

                            _logger.Log(Abstraction.Layer.Business().Routine(nameof(RunAsync)).Completed());
                        }
                        catch (DynamicException ex) when (ex.NameMatches("^DataSource"))
                        {
                            _logger.Log(Abstraction.Layer.Business().Routine(nameof(RunAsync)).Faulted(), ex);
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

        private (TestResult Result, TestRunnerActions Actions) RunTest(TestCase testCase, DataTable data)
        {
            if (!(data.Compute(testCase.Assert, testCase.Filter) is bool result))
            {
                throw new InvalidOperationException($"'{nameof(TestCase.Assert)}' must evaluate to '{nameof(Boolean)}'.");
            }

            var testResult = result ? TestResult.Passed : TestResult.Failed;

            _logger.Log(Abstraction.Layer.Infrastructure().Meta(new { Result = testResult }));

            var alert =
                (testResult.Passed() && testCase.OnPassed.Alert()) ||
                (testResult.Failed() && testCase.OnFailed.Alert());

            var halt =
                (testResult.Passed() && testCase.OnPassed.Halt()) ||
                (testResult.Failed() && testCase.OnFailed.Halt());

            var actions = TestRunnerActions.None;

            if (alert) actions |= TestRunnerActions.Alert;
            if (halt) actions |= TestRunnerActions.Halt;

            return (testResult, actions);
        }

        private static async Task AlertAsync(TestContext context)
        {
            foreach (var message in context.TestCase.Messages(context.TestBundle))
            {
                await message.PublishAsync(context);
            }
        }
    }

    public static class TestResultExtensions
    {
        public static bool Passed(this TestResult result) => result == TestResult.Passed;

        public static bool Failed(this TestResult result) => result == TestResult.Failed;
    }

    public static class TestRunnerActionExtensions
    {
        public static bool Halt(this TestRunnerActions actions) => actions.HasFlag(TestRunnerActions.Halt);

        public static bool Alert(this TestRunnerActions actions) => actions.HasFlag(TestRunnerActions.Alert);
    }
}