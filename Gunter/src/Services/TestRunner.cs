using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Data;
using Gunter.Extensions;
using JetBrains.Annotations;
using Reusable;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.Reflection;
using Reusable.SmartConfig;

namespace Gunter.Services
{
    public interface ITestRunner
    {
        Task RunTestsAsync(string path, IEnumerable<string> profiles);
    }

    [UsedImplicitly]
    internal class TestRunner : ITestRunner
    {
        private readonly RuntimeFormatter.Factory _createRuntimeFormatter;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly ITestLoader _testLoader;
        private readonly ITestComposer _testComposer;

        public TestRunner
        (
            ILogger<TestRunner> logger,
            IConfiguration configuration,
            ITestLoader testLoader,
            ITestComposer testComposer,
            RuntimeFormatter.Factory createRuntimeFormatter
        )
        {
            _createRuntimeFormatter = createRuntimeFormatter;
            _logger = logger;
            _configuration = configuration;
            _testLoader = testLoader;
            _testComposer = testComposer;
        }

        public async Task RunTestsAsync(string path, IEnumerable<string> profiles)
        {
            var tests = _testLoader.LoadTests(path).ToList();
            var compositions = _testComposer.ComposeTests(tests).ToList();
            var tasks = compositions.Select(async testFile => await RunTestsAsync(testFile, profiles.Select(SoftString.Create))).ToArray();
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

            using (_logger.BeginScope().WithCorrelationContext(new { TestBundle = testBundle.FileName }).AttachElapsed())
            using (Disposable.Create(() => { foreach (var (data, _, _) in cache.Values) { data.Dispose(); } }))
            {
                foreach (var current in tests)
                {
                    using (_logger.BeginScope().WithCorrelationContext(new { TestCase = current.testCase.Id }).AttachElapsed())
                    {
                        try
                        {
                            if (!cache.TryGetValue(current.dataSource.Id, out var cacheItem))
                            {
                                var getDataStopwatch = Stopwatch.StartNew();
                                var (data, query) = await current.dataSource.GetDataAsync(testBundle.Directoryname, testBundleFormatter);
                                cache[current.dataSource.Id] = cacheItem = (data, query, getDataStopwatch.Elapsed);
                            }

                            var (result, elapsed, actions) = RunTest(current.testCase, cacheItem.Data);

                            _logger.Log(Abstraction.Layer.Infrastructure().Meta(new
                            {
                                Test = new
                                {
                                    current.testCase.Id,
                                    Result = result,
                                    Elapsed = elapsed.ToString(@"mm\:ss\.fff"),
                                    Actions = actions
                                }
                            }));

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
                                                RunTestElapsed = elapsed
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
                                    Query = cacheItem.Query
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

        private static (TestResult Result, TimeSpan Elapsed, TestRunnerActions Actions) RunTest(TestCase testCase, DataTable data)
        {
            var assertStopwatch = Stopwatch.StartNew();
            if (!(data.Compute(testCase.Expression, testCase.Filter) is bool result))
            {
                throw new InvalidOperationException($"Expression must evaluate to {nameof(Boolean)}.");
            }

            var testResult = result == testCase.Assert ? TestResult.Passed : TestResult.Failed;

            var alert =
                (testResult.Passed() && testCase.OnPassed.Alert()) ||
                (testResult.Failed() && testCase.OnFailed.Alert());

            var halt =
                (testResult.Passed() && testCase.OnPassed.Halt()) ||
                (testResult.Failed() && testCase.OnFailed.Halt());

            var actions = TestRunnerActions.None;

            if (alert) actions |= TestRunnerActions.Alert;
            if (halt) actions |= TestRunnerActions.Halt;

            return (testResult, assertStopwatch.Elapsed, actions);
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
