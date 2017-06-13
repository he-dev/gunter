using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Gunter.Data;
using Reusable.Logging;
using Gunter.Services;
using Gunter.Extensions;
using System.Threading.Tasks;
using Gunter.Reporting;
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

        public void RunTestFiles(IEnumerable<TestFile> testFiles, IVariableResolver variables)
        {
            //LogEntry.New().Debug().Message($"Test configuration count: {tests.Count}").Log(_logger);

            var testUnitGroups =
                (from testFile in testFiles
                 let testFileVariables = variables.MergeWith(_variableBuilder.BuildVariables(testFile))
                 select TestComposer.ComposeTests(testFile, testFileVariables)).ToList();

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

        public void RunTests(IEnumerable<TestUnit> testUnits)
        {
            //LogEntry.New().Info().Message($"Executing \"{Path.GetFileNameWithoutExtension(tuple.FileName)}\".").Log(_logger);
            var testFileEntry = default(LogEntry);

            var counter = 1;
            foreach (var testUnit in testUnits.Where(testUnit => testUnit.Test.Enabled))
            {
                testFileEntry = testFileEntry ?? LogEntry.New().Stopwatch(sw => sw.Start()).Message("{TestFile.FileName}");

                var testUnitEntry = LogEntry.New().Stopwatch(sw => sw.Start());
                try
                {
                    if (testUnit.DataSource.Data.Compute(testUnit.Test.Expression, testUnit.Test.Filter) is bool testResult)
                    {
                        //LogEntry.New().Info().Message($"{(success ? "Success" : "Failure")}");

                        var success = testResult == testUnit.Test.Assert;


                        var mustAlert =
                            (success && testUnit.Test.AlertTrigger == AlertTrigger.Success) ||
                            (!success && testUnit.Test.AlertTrigger == AlertTrigger.Failure);

                        if (mustAlert)
                        {
                            var testVariables = testUnit.Test.Variables.MergeWith(_variableBuilder.BuildVariables(testUnit.Test));
                            foreach (var alert in testUnit.Alerts)
                            {
                                alert.UpdateVariables(testVariables);
                                alert.Publish(testUnit);
                            }
                        }

                        if (!success && !testUnit.Test.ContinueOnFailure)
                        {
                            return;
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"Test expression must evaluate to {nameof(Boolean)}.");
                    }
                }
                catch (Exception ex)
                {
                    LogEntry.New().Error().Exception(ex).Message($"Inconclusive. The expression must evaluate to {nameof(Boolean)}.");
                }
                finally
                {
                    testUnitEntry.Message($"Test completed.").Log(_logger);
                }
            }
        }
    }
}
