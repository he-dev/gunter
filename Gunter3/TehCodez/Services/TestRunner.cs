using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Gunter.Data;
using Gunter.Services;
using Gunter.Extensions;
using System.Threading.Tasks;
using Gunter.Reporting;
using Gunter.Services.Validators;
using JetBrains.Annotations;
using Reusable.Extensions;
using Reusable.OmniLog;

namespace Gunter.Services
{
    internal interface ITestRunner
    {
        void RunTests(
            TestFile globalTestFile,
            IEnumerable<TestFile> testFiles,
            IEnumerable<string> runnableProfiles);
    }

    [UsedImplicitly]
    internal class TestRunner
    {
        private readonly ILogger _logger;
        private readonly IRuntimeFormatter _runtimeFormatter;
        private readonly IEnumerable<IRuntimeVariable> _runtimeVariables;

        public TestRunner(
            ILoggerFactory loggerFactory,
            IRuntimeFormatter runtimeFormatter,
            IEnumerable<IRuntimeVariable> runtimeVariables)
        {
            _logger = loggerFactory.CreateLogger(nameof(TestRunner));
            _runtimeFormatter = runtimeFormatter;
            _runtimeVariables = runtimeVariables;

#if DEBUG
            MaxDegreeOfParallelism = 1;
#endif
        }

        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

        public void RunTests(
            TestFile globalTestFile,
            IEnumerable<TestFile> testFiles,
            IEnumerable<string> profiles)
        {
            //_logger.Log(e => e.Info().Message($"Profiles: [{string.Join(", ", runnableProfiles)}]"));

            Parallel.ForEach
            (
                source: testFiles,
                parallelOptions: new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism },
                body: testFile => RunTests(globalTestFile, testFile, profiles)
            );
        }

        private void RunTests(TestFile globalTestFile, TestFile testFile, IEnumerable<string> profiles)
        {
            var localVariables =
                globalTestFile.Locals.Concat(testFile.Locals)
                    .GroupBy(x => x.Key)
                    .Select(g => g.Last())
                    .ToDictionary(x => x.Key, x => x.Value);

            VariableValidator.ValidateNamesNotReserved(localVariables, _runtimeVariables.Select(x => x.Name));

            var cache = new ConcurrentDictionary<int, (DataTable Value, TimeSpan Elapsed)>();

            var tests =
                from testCase in testFile.Tests
                where testCase.CanExecute(profiles)
                from dataSource in testCase.DataSources(testFile)
                select (testCase, dataSource);

            try
            {
                foreach (var current in tests)
                {
                    var testFileVariables = _runtimeVariables.Resolve(testFile);
                    var testCaseVariables = _runtimeVariables.Resolve(current.testCase);
                    var dataSourceVariables = _runtimeVariables.Resolve(current.dataSource);

                    var formatter =
                        _runtimeFormatter
                            .AddRange(localVariables)
                            .AddRange(testFileVariables)
                            .AddRange(testCaseVariables)
                            .AddRange(dataSourceVariables);

                    var data = cache.GetOrAdd(current.dataSource.Id, id =>
                    {
                        var getDataStopwatch = Stopwatch.StartNew();
                        return (current.dataSource.GetData(formatter), getDataStopwatch.Elapsed);
                    });

                    if (data.Value is null)
                    {
                        // Faulted.
                        continue;
                    }

                    try
                    {
                        var computeStopwatch = Stopwatch.StartNew();
                        if (data.Value.Compute(current.testCase.Expression, current.testCase.Filter) is bool result)
                        {
                            computeStopwatch.Stop();


                            var testResult = result == current.testCase.Assert ? TestResult.Passed : TestResult.Failed;

                            //_logger.Log(e => e.Message($"Test {testUnit.TestNumber} in {testUnit.FileName} {testResult.ToString().ToUpper()}."));

                            var mustAlert =
                                (testResult == TestResult.Passed && current.testCase.OnPassed.HasFlag(TestResultActions.Alert)) ||
                                (testResult == TestResult.Failed && current.testCase.OnFailed.HasFlag(TestResultActions.Alert));

                            if (mustAlert)
                            {
                                foreach (var alert in current.testCase.Alerts(testFile))
                                {
                                    alert.Publish(new TestContext
                                    {
                                        TestFile = testFile,
                                        TestCase = current.testCase,
                                        DataSource = current.dataSource,
                                        Data = data.Value,
                                        GetDataElapsed = data.Elapsed,
                                        RunTestElapsed = computeStopwatch.Elapsed,
                                        Formatter = formatter
                                    });

                                    //_logger.Log(e => e.Message($"Published alert {alert.Id} for test {testUnit.TestNumber} in {testUnit.FileName}."));
                                }
                            }

                            var mustHalt =
                                (testResult == TestResult.Passed && current.testCase.OnPassed.HasFlag(TestResultActions.Halt)) ||
                                (testResult == TestResult.Failed && current.testCase.OnFailed.HasFlag(TestResultActions.Halt));

                            if (mustHalt)
                            {
                                //_logger.Log(e => e.Message($"Halt at test {testUnit.TestNumber} in {testUnit.FileName}."));
                                return;
                            }
                        }
                        else
                        {
                            //throw new InvalidOperationException($"Test expression must evaluate to {nameof(Boolean)}. Affected test {testUnit.TestNumber} in {testUnit.FileName}.");
                        }
                    }
                    catch (Exception ex)
                    {
                        //_logger.Log(e => e.Error().Exception(ex).Message($"Could not run test '{testUnit.FileName}'."));
                    }
                    finally
                    {
                        //testUnitLogger.LogEntry.Message($"Test {testUnit.TestNumber} in {testUnit.FileName} completed.");
                        //testUnitLogger.EndLog();
                    }

                }
            }
            catch (Exception e)
            {

            }
            finally
            {
                foreach (var dataTable in cache.Values)
                {
                    dataTable.Value?.Dispose();
                }
                cache.Clear();
            }
        }
    }


}
