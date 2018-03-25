using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Data;
using JetBrains.Annotations;
using Reusable;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter
{
    [UsedImplicitly]
    internal class TestRunner : ITestRunner
    {
        private readonly ILogger _logger;
        private readonly IRuntimeFormatterFactory _runtimeFormatterFactory;

        public TestRunner(
            ILoggerFactory loggerFactory,
            IRuntimeFormatterFactory runtimeFormatterFactory)
        {
            _logger = loggerFactory.CreateLogger(nameof(TestRunner));
            _runtimeFormatterFactory = runtimeFormatterFactory;
        }

        public async Task RunTestsAsync(TestFile testFile, IEnumerable<SoftString> profiles)
        {
            //VariableValidator.ValidateNamesNotReserved(localVariables, _runtimeVariables.Select(x => x.Name));

            var cache = new Dictionary<int, (DataTable Value, TimeSpan Elapsed)>();

            var testIndex = 0;
            var tests =
                from testCase in testFile.Tests
                where testCase.CanExecute(profiles)
                from dataSource in testCase.DataSources(testFile)
                select (testCase, dataSource, testIndex: testIndex++);

            var scope = _logger.BeginScope(nameof(RunTestAsync), new { testFile.FileName }).AttachElapsed();
            try
            {
                foreach (var current in tests)
                {
                    try
                    {
                        var canContinue = await RunTestAsync(testFile, current, cache);
                        if (!canContinue)
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(Abstraction.Layer.Business().Action().Failed(nameof(RunTestAsync)), log => log.Exception(ex));
                    }
                }
                _logger.Log(Abstraction.Layer.Business().Action().Finished(nameof(RunTestsAsync)));
            }
            catch (Exception ex)
            {
                _logger.Log(Abstraction.Layer.Business().Action().Failed(nameof(RunTestsAsync)), log => log.Exception(ex));
            }
            finally
            {
                scope.Dispose();
                foreach (var (value, _) in cache.Values)
                {
                    value?.Dispose();
                }
                cache.Clear();
            }
        }

        private async Task<bool> RunTestAsync(TestFile testFile, (TestCase testCase, IDataSource dataSource, int testIndex) current, IDictionary<int, (DataTable Value, TimeSpan GetDataElapsed)> cache)
        {
            _logger.Log(Abstraction.Layer.Business().Data().Argument(new { testFile = testFile.FileName, current.testIndex }));

            const bool canContinue = true;

            var localFormatter =
                _runtimeFormatterFactory
                    .Create(
                        testFile.Locals
                    );

            if (!cache.TryGetValue(current.dataSource.Id, out var data))
            {
                var getDataStopwatch = Stopwatch.StartNew();
                var value = await current.dataSource.GetDataAsync(localFormatter);
                cache[current.dataSource.Id] = data = (value, getDataStopwatch.Elapsed);
                //_logger.Log(Abstraction.Layer.Database().Data().Object(new { getDataStopwatch = getDataStopwatch.Elapsed.ToString(Program.ElapsedFormat) }));
                _logger.Log(Abstraction.Layer.Database().Data().Metric(new { GetDataAsync = new { RowCount = value?.Rows.Count, Elapsed = getDataStopwatch.Elapsed.TotalMilliseconds } }));
            }

            if (data.Value is null)
            {
                // Faulted.
                return canContinue;
            }

            var assertStopwatch = Stopwatch.StartNew();
            if (data.Value.Compute(current.testCase.Expression, current.testCase.Filter) is bool result)
            {
                assertStopwatch.Stop();
                var testResult = result == current.testCase.Assert ? TestResult.Passed : TestResult.Failed;

                _logger.Log(Abstraction.Layer.Business().Data().Variable(new { testResult }));
                _logger.Log(Abstraction.Layer.Business().Data().Object(new { assertStopwatch = assertStopwatch.Elapsed.ToString(Program.ElapsedFormat) }));
                _logger.Log(Abstraction.Layer.Business().Action().Finished("DataTable.Compute"));

                var mustAlert =
                    (testResult.Passed() && current.testCase.OnPassed.Alert()) ||
                    (testResult.Failed() && current.testCase.OnFailed.Alert());

                _logger.Log(Abstraction.Layer.Business().Data().Variable(new { mustAlert }));

                if (mustAlert)
                {
                    var runtimeFormatter =
                        _runtimeFormatterFactory
                            .Create(
                                testFile.Locals,
                                testFile,
                                current.testCase,
                                current.dataSource,
                                new TestStatistic
                                {
                                    GetDataElapsed = data.GetDataElapsed,
                                    AssertElapsed = assertStopwatch.Elapsed
                                },
                                typeof(Program)
                            );

                    foreach (var message in current.testCase.Messages(testFile))
                    {
                        await message.PublishAsync(new TestContext
                        {
                            TestFile = testFile,
                            TestCase = current.testCase,
                            DataSource = current.dataSource,
                            Data = data.Value,
                            Formatter = runtimeFormatter
                        });

                        //_logger.Log(e => e.Message($"Published alert {alert.Id} for test {testUnit.TestNumber} in {testUnit.FileName}."));
                    }
                }

                var mustHalt =
                    (testResult.Passed() && current.testCase.OnPassed.Halt()) ||
                    (testResult.Failed() && current.testCase.OnFailed.Halt());

                _logger.Log(Abstraction.Layer.Business().Data().Variable(new { mustHalt }));

                if (mustHalt)
                {
                    return !canContinue;
                }
            }
            else
            {
                throw new InvalidOperationException($"Expression must evaluate to {nameof(Boolean)}.");
            }

            return canContinue;
        }
    }

    public static class TestRunnerExtensions
    {
        public static void RunTests(this ITestRunner testRunner, IEnumerable<TestFile> testFiles, IEnumerable<SoftString> profiles)
        {
            //_logger.Log(e => e.Info().Message($"Profiles: [{string.Join(", ", runnableProfiles)}]"));

#if DEBUG
            //var maxDegreeOfParallelism = 1;
#else
            //var maxDegreeOfParallelism = Environment.ProcessorCount;
#endif

            var tasks = testFiles.Select(testFile => testRunner.RunTestsAsync(testFile, profiles)).ToArray();

            Task.WaitAll(tasks);

            //Parallel.ForEach
            //(
            //    source: testFiles,
            //    parallelOptions: new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
            //    body: testFile => testRunner.RunTestsAsync(testFile, profiles)
            //);
        }
    }

    public static class TestResultExtensions
    {
        public static bool Passed(this TestResult testResult) => testResult == TestResult.Passed;

        public static bool Failed(this TestResult testResult) => testResult == TestResult.Failed;
    }

    public static class TestActionExtensions
    {
        public static bool Halt(this TestActions testActions) => testActions.HasFlag(TestActions.Halt);

        public static bool Alert(this TestActions testActions) => testActions.HasFlag(TestActions.Alert);
    }
}
