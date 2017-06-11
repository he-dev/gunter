using System;
using System.Collections.Generic;
using System.Data;
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
        private readonly ILogger _logger;

        public TestRunner(ILogger logger)
        {
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
                 select TestComposer.ComposeTests(testFile, variables)).ToList();

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
            foreach (var testUnit in testUnits.Where(tu => tu.Test.Enabled))
            {
                var logEntry =
                    LogEntry
                        .New();
                //.SetValue(nameof(TestCase.Expression), testCase.config.Test.Expression)
                //.SetValue(VariableName.TestCollection.FileName, testCase.config.Constants.Resolve(VariableName.TestCollection.FileName.ToFormatString()));

                try
                {
                    if (testUnit.DataSource.Data.Compute(testUnit.Test.Expression, testUnit.Test.Filter) is bool testResult)
                    {
                        var success = testResult == testUnit.Test.Assert;

                        logEntry.Info().Message($"{(success ? "Success" : "Failure")}");

                        var mustAlert =
                            (success && testUnit.Test.AlertTrigger == AlertTrigger.Success) ||
                            (!success && testUnit.Test.AlertTrigger == AlertTrigger.Failure);

                        if (mustAlert)
                        {
                            foreach (var alert in testUnit.Alerts)
                            {
                                alert.UpdateVariables(testUnit.Test.Variables);
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
                    logEntry
                        .Error()
                        .Exception(ex)
                        .Message($"Inconclusive. The expression must evaluate to {nameof(Boolean)}.");
                }
                finally
                {
                    logEntry.Log(_logger);
                }
            }
        }
    }
}
