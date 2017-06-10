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

        public void RunTestFiles(IEnumerable<TestFile> testFiles, IConstantResolver constants)
        {
            //LogEntry.New().Debug().Message($"Test configuration count: {tests.Count}").Log(_logger);

            var testConfigs =
                from testFile in testFiles
                select TestComposer.ComposeTests(testFile);

            Parallel.ForEach
            (
                source: testConfigs,
                parallelOptions: new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism },
                body: testConfig => RunTests(testConfig, constants)
            );
        }

        public void RunTests(IEnumerable<TestConfiguration> testConfigurations, IConstantResolver constants)
        {
            var testCases =
                from config in testConfigurations
                from dataSource in config.DataSources
                let locals = constants.UnionWith(config.Locals)
                //let testConstants = locals
                //    .Add(VariableName.TestFile.FileName, config.FileName)
                //    .Add(VariableName.TestCase.Severity, config.Test.Severity)
                //    .Add(VariableName.TestCase.Message, config.Test.Message)
                select new
                {
                    Test = config.Test.UpdateConstants(locals),
                    DataSource = dataSource.UpdateConstants(locals),
                    Alerts = config.Alerts,
                    Reports = config.Reports
                };

            foreach (var testCase in testCases)
            {
                var logEntry =
                    LogEntry
                        .New();
                //.SetValue(nameof(TestCase.Expression), testCase.config.Test.Expression)
                //.SetValue(VariableName.TestCollection.FileName, testCase.config.Constants.Resolve(VariableName.TestCollection.FileName.ToFormatString()));

                try
                {
                    using (var data = testCase.DataSource.GetData())
                    {
                        if (data.Compute(testCase.Test.Expression, testCase.Test.Filter) is bool testResult)
                        {
                            var success = testResult == testCase.Test.Assert;

                            logEntry.Info().Message($"{(success ? "Success" : "Failure")}");

                            var mustAlert =
                                (success && testCase.Test.AlertTrigger == AlertTrigger.Success) ||
                                (!success && testCase.Test.AlertTrigger == AlertTrigger.Failure);

                            if (mustAlert)
                            {
                                var testContext = new TestContext
                                {
                                    Test = testCase.Test,
                                    DataSource = testCase.DataSource,
                                    Data = data,
                                    Alerts = testCase.Alerts,
                                    Reports = testCase.Reports,
                                    Constants = testCase.constants
                                };

                                foreach (var alert in testCase.config.Alerts)
                                {
                                    alert.Publish(testContext);
                                }
                            }

                            if (!success && !testCase.config.Test.ContinueOnFailure)
                            {
                                return;
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException($"Test expression must evaluate to {nameof(Boolean)}.");
                        }
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
