using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Gunter.Data;
using Reusable.Logging;
using Gunter.Services;
using Gunter.Extensions;
using System.Threading.Tasks;
using Gunter.Reporting;
using JetBrains.Annotations;
using Reusable.Extensions;

namespace Gunter.Services
{
    internal class TestRunner
    {
        private readonly IVariableBuilder _variableBuilder;
        private readonly ILogger _logger;

        public TestRunner(ILogger logger, IVariableBuilder variableBuilder)
        {
            _variableBuilder = variableBuilder;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

#if DEBUG
            MaxDegreeOfParallelism = 1;
#endif
        }

        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

        public void RunTestFiles(
            [NotNull, ItemNotNull] IEnumerable<TestFile> testFiles,
            [NotNull, ItemNotNull] ICollection<string> runnableProfiles,
            [NotNull] IVariableResolver variables)
        {
            //LogEntry.New().Debug().Message($"Test configuration count: {tests.Count}").Log(_logger);

            var testUnitGroups =
                (from testFile in testFiles
                 let testFileVariables = variables.MergeWith(_variableBuilder.BuildVariables(testFile))
                 let testUnits = TestComposer.ComposeTests(testFile, testFileVariables)
                 select GetRunnableTestUnits(testUnits, runnableProfiles)).ToList();

            try
            {
                Parallel.ForEach
                (
                    source: testUnitGroups,
                    parallelOptions: new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism },
                    body: RunTests
                );
            }
            finally
            {
                foreach (var testUnit in testUnitGroups.SelectMany(testUnits => testUnits))
                {
                    testUnit.Dispose();
                }
            }
        }

        [NotNull, ItemNotNull]
        private static IEnumerable<TestUnit> GetRunnableTestUnits(
            [NotNull, ItemNotNull] IEnumerable<TestUnit> testUnits,
            [NotNull, ItemNotNull] ICollection<string> runnableProfiles)
        {
            var runnableTestUnits =
                from testUnit in testUnits
                where
                    testUnit.TestCase.Enabled &&
                    ProfileMatches(testUnit.TestCase.Profiles)
                select testUnit;

            return runnableTestUnits;

            bool ProfileMatches(IEnumerable<string> profiles)
            {
                return
                    runnableProfiles.Count == 0 ||
                    runnableProfiles.Any(runnableProfile => profiles.Contains(runnableProfile, StringComparer.OrdinalIgnoreCase));
            }
        }

        private void RunTests([NotNull, ItemNotNull] IEnumerable<TestUnit> testUnits)
        {
            var testFileEntry = default(LogEntry);

            foreach (var testUnit in testUnits)
            {
                testFileEntry = testFileEntry ?? LogEntry.New().Stopwatch(sw => sw.Start());

                var testUnitEntry = LogEntry.New().Stopwatch(sw => sw.Start());
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    if (testUnit.DataSource.Data.Compute(testUnit.TestCase.Expression, testUnit.TestCase.Filter) is bool result)
                    {
                        stopwatch.Stop();
                        testUnit.TestCase.Elapsed = stopwatch.Elapsed;

                        var testResult = result == testUnit.TestCase.Assert ? TestResult.Passed : TestResult.Failed;

                        LogEntry.New().Info().Message($"Test {testUnit.TestNumber} in {testUnit.FileName} {testResult.ToString().ToUpper()}.").Log(_logger);

                        var mustAlert =
                            (testResult == TestResult.Passed && testUnit.TestCase.OnPassed.HasFlag(TestResultActions.Alert)) ||
                            (testResult == TestResult.Failed && testUnit.TestCase.OnFailed.HasFlag(TestResultActions.Alert));

                        if (mustAlert)
                        {
                            var testVariables = testUnit.TestCase.Variables
                                .MergeWith(_variableBuilder.BuildVariables(testUnit.TestCase))
                                .MergeWith(_variableBuilder.BuildVariables(testUnit.DataSource));

                            foreach (var alert in testUnit.Alerts)
                            {
                                alert.UpdateVariables(testVariables);
                                alert.Publish(testUnit);

                                LogEntry.New().Info().Message($"Published alert {alert.Id} for test {testUnit.TestNumber} in {testUnit.FileName}.").Log(_logger);
                            }
                        }

                        var mustHalt =
                            (testResult == TestResult.Passed && testUnit.TestCase.OnPassed.HasFlag(TestResultActions.Halt)) ||
                            (testResult == TestResult.Failed && testUnit.TestCase.OnFailed.HasFlag(TestResultActions.Halt));

                        if (mustHalt)
                        {
                            LogEntry.New().Info().Message($"Halt at test {testUnit.TestNumber} in {testUnit.FileName}.").Log(_logger);
                            return;
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"Test expression must evaluate to {nameof(Boolean)}. Affected test {testUnit.TestNumber} in {testUnit.FileName}.");
                    }
                }
                catch (Exception ex)
                {
                    LogEntry.New().Error().Exception(ex).Message(ex.Message).Log(_logger);
                }
                finally
                {
                    testUnitEntry.Message($"Test {testUnit.TestNumber} in {testUnit.FileName} completed.").Log(_logger);
                }
            }
        }
    }
}
