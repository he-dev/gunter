using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Data;
using JetBrains.Annotations;
using Reusable;
using Reusable.Exceptionize;
using Reusable.Extensions;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.SmartConfig;
using Reusable.SmartConfig.Utilities;

namespace Gunter
{
    public interface ITestRunner
    {
        Task RunTestsAsync(TestBundle testBundle, IEnumerable<SoftString> profiles);
    }

    [UsedImplicitly]
    internal class TestRunner : ITestRunner
    {
        private readonly RuntimeFormatter.Factory _createRuntimeFormatter;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public TestRunner(
            ILogger<TestRunner> logger,
            IConfiguration configuration,
            RuntimeFormatter.Factory createRuntimeFormatter)
        {
            _createRuntimeFormatter = createRuntimeFormatter;
            _logger = logger;
            _configuration = configuration;
            _logger.Log(Abstraction.Layer.Infrastructure().Meta(new { foo = "bar" }));
        }

        public async Task RunTestsAsync(TestBundle testBundle, IEnumerable<SoftString> profiles)
        {
            //VariableValidator.ValidateNamesNotReserved(localVariables, _runtimeVariables.Select(x => x.Name));
            _logger.Log(Abstraction.Layer.Business().Argument(new { testBundle = new { testBundle.FileName } }));

            var testIndex = 0;
            var tests =
                from testCase in testBundle.Tests
                where testCase.CanExecute(profiles)
                from dataSource in testCase.DataSources(testBundle)
                select (testCase, dataSource, testIndex: testIndex++);

            var testBundleFormatter = _createRuntimeFormatter(testBundle.Variables, runtimeObjects: new object[]
            {
                testBundle,
            });

            using (var scope = _logger.BeginScope().AttachElapsed())
            using (var cache = new TestBundleDataCache())
            {
                foreach (var current in tests)
                {
                    try
                    {
                        var cacheItem = await GetDataAsync(current.dataSource, testBundleFormatter, cache);
                        var result = RunTest(current.testCase, cacheItem.Data);

                        _logger.Log(Abstraction.Layer.Business().Variable(new { test = new { result.Result, Elapsed = result.Elapsed.ToString(_configuration.GetValue<string>("ElapsedFormat")), result.Actions } }));

                        if (result.Actions.Alert())
                        {
                            var testCaseFormatter =
                                _createRuntimeFormatter(
                                    variables: testBundle.Variables,
                                    runtimeObjects: new object[]
                                    {
                                        testBundle,
                                        current.testCase,
                                        current.dataSource,
                                        new TestCounter
                                        {
                                            GetDataElapsed = cacheItem.Elapsed,
                                            RunTestElapsed = result.Elapsed
                                        },
                                    }
                                );

                            await AlertAsync(new TestContext
                            {
                                TestBundle = testBundle,
                                TestCase = current.testCase,
                                DataSource = current.dataSource,
                                Data = cacheItem.Data,
                                Formatter = testCaseFormatter
                            });
                        }

                        if (result.Actions.Halt())
                        {
                            break;
                        }

                        _logger.Log(Abstraction.Layer.Business().Routine(nameof(RunTestsAsync)).Completed());
                    }
                    catch (DynamicException ex) when (ex.NameEquals("DataSourceException"))
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

        private async Task<(DataTable Data, TimeSpan Elapsed)> GetDataAsync(IDataSource dataSource, IRuntimeFormatter formatter, TestBundleDataCache cache)
        {
            if (!cache.TryGetValue(dataSource.Id, out var cacheItem))
            {
                var getDataStopwatch = Stopwatch.StartNew();
                var value = await dataSource.GetDataAsync(formatter);
                cache[dataSource.Id] = cacheItem = (value, getDataStopwatch.Elapsed);
                //_logger.Log(Abstraction.Layer.Database().Data().Object(new { getDataStopwatch = getDataStopwatch.Elapsed.ToString(Program.ElapsedFormat) }));
                //_logger.Log(Abstraction.Layer.Database().Data().Metric(new { GetDataAsync = new { RowCount = value?.Rows.Count, Elapsed = getDataStopwatch.Elapsed.TotalMilliseconds } }));
            }

            return cacheItem;
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

    //internal class 

    internal class TestBundleDataCache : Dictionary<int, (DataTable Data, TimeSpan Elapsed)>, IDisposable
    {
        public void Dispose()
        {
            foreach (var (data, _) in Values)
            {
                data?.Dispose();
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
