using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
        Task RunTestsAsync
        (
            [NotNull] string testsPath,
            [CanBeNull] IEnumerable<TestFilter> filters,
            [CanBeNull] IEnumerable<SoftString> profiles
        );
    }

    [UsedImplicitly]
    internal class TestRunner : ITestRunner
    {
        private readonly RuntimeFormatter.Factory _createRuntimeFormatter;
        private readonly ILogger _logger;
        private readonly ITestLoader _testLoader;
        private readonly ITestComposer _testComposer;

        public TestRunner
        (
            ILogger<TestRunner> logger,
            IResourceProvider resourceProvider,
            ITestLoader testLoader,
            ITestComposer testComposer,
            RuntimeFormatter.Factory createRuntimeFormatter
        )
        {
            _createRuntimeFormatter = createRuntimeFormatter;
            _logger = logger;
            _testLoader = testLoader;
            _testComposer = testComposer;
        }

        public async Task RunTestsAsync(string testsPath, IEnumerable<TestFilter> filters, IEnumerable<SoftString> profiles)
        {
            var bundles = await _testLoader.LoadTestsAsync(testsPath);
            var compositions = _testComposer.ComposeTests(bundles, filters).ToList();
            var tasks = compositions.Select(async testFile => await RunTestsAsync(testFile, profiles)).ToArray();
            await Task.WhenAll(tasks).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    _logger.Log(Abstraction.Layer.Infrastructure().Routine(nameof(RunTestsAsync)).Faulted(), task.Exception);
                }
            });
        }

        private async Task RunTestsAsync(TestBundle testBundle, IEnumerable<SoftString> profiles)
        {
            var testIndex = 0;
            var tests =
                from testCase in testBundle.Tests
                where testCase.CanExecute(profiles)
                from dataSource in testCase.DataSources(testBundle)
                select (testCase, dataSource, testIndex: testIndex++);

            var testBundleFormatter = _createRuntimeFormatter(testBundle.AllVariables(), new object[] { testBundle });

            var cache = new Dictionary<SoftString, (DataTable Data, string Query, TimeSpan Elapsed)>();

            using (_logger.BeginScope().WithCorrelationHandle("TestBundle").AttachElapsed())
            using (Disposable.Create(() =>
            {
                foreach (var (data, _, _) in cache.Values) { data.Dispose(); }
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
                                var (data, query) = await current.dataSource.GetDataAsync(testBundle.Directoryname, testBundleFormatter);
                                cache[current.dataSource.Id] = cacheItem = (data, query, getDataStopwatch.Elapsed);
                            }

                            var assertStopwatch = Stopwatch.StartNew();
                            var (result, actions) = RunTest(current.testCase, cacheItem.Data);
                            var assertElapsed = assertStopwatch.Elapsed;                            

                            if (actions.Alert())
                            {
                                var testCaseFormatter =
                                    _createRuntimeFormatter(
                                        variables: testBundle.AllVariables(),
                                        runtimeObjects: new object[]
                                        {
                                            testBundle,
                                            current.testCase,
                                            current.dataSource,
                                            new TestCounter
                                            {
                                                GetDataElapsed = cacheItem.Elapsed,
                                                RunTestElapsed = assertElapsed
                                            },
                                        }
                                    );

                                await AlertAsync(new TestContext
                                {
                                    TestBundle = testBundle,
                                    TestCase = current.testCase,
                                    DataSource = current.dataSource,
                                    Data = cacheItem.Data,
                                    Formatter = testCaseFormatter,
                                    Query = cacheItem.Query,
                                    Result = result
                                });
                            }

                            if (actions.Halt())
                            {
                                break;
                            }

                            _logger.Log(Abstraction.Layer.Business().Routine(nameof(RunTestsAsync)).Completed());
                        }
                        catch (DynamicException ex) when (ex.NameMatches("^DataSource"))
                        {
                            _logger.Log(Abstraction.Layer.Business().Routine(nameof(RunTestsAsync)).Faulted(), ex);
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.Log(Abstraction.Layer.Business().Routine(nameof(RunTestsAsync)).Faulted(), ex);
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

    public class TestFilter
    {
        public TestFilter(string name)
        {
            Name = name;
        }

        [NotNull]
        public SoftString Name { get; }

        [CanBeNull, ItemNotNull]
        public IEnumerable<SoftString> Ids { get; set; }
    }
}