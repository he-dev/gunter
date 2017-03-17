using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Gunter.Data;
using Gunter.Alerts;
using Reusable.Logging;
using Gunter.Services;
using Gunter.Extensions;

namespace Gunter.Testing
{
    internal class TestRunner
    {
        private readonly ILogger _logger;

        public TestRunner(ILogger logger)
        {
            _logger = logger;
        }

        public void RunTests(IList<TestConfiguration> testConfigs, string profile, IConstantResolver constants)
        {
            foreach (var testConfig in testConfigs.Where(t => t.ContainsProfile(profile)))
            {
                RunTests(testConfig, constants);
            }
        }

        public void RunTests(TestConfiguration testConfig, IConstantResolver constants)
        {
            foreach (var test in testConfig.GetEnabledTests())
            {
                var locals = constants
                    .UnionWith(testConfig.Locals)
                    .Add(Globals.Test.Severity, test.Severity)
                    .Add(Globals.Test.FileName, testConfig.FileName)
                    .Add(Globals.Test.Message, test.Message);

                var dataSources = testConfig.GetDataSources(test.DataSources).ToList();
                if (!dataSources.Any())
                {
                    LogEntry
                        .New()
                        .Warn()
                        .Message("Data source not found.")
                        .SetValue(nameof(TestProperties.Name), test.Name)
                        .SetValue(Globals.Test.FileName, locals.Resolve(Globals.Test.FileName.ToFormatString()))
                        .Log(_logger);
                    continue;
                }

                foreach (var dataSource in dataSources)
                {
                    using (var data = dataSource.GetData(locals))
                    using (var logEntry = LogEntry.New().SetValue(nameof(TestProperties.Name), test.Name).SetValue(Globals.Test.FileName, locals.Resolve(Globals.Test.FileName.ToFormatString())).AsAutoLog(_logger))
                    {
                        switch (Assert(data, test, locals))
                        {
                            case true:
                                logEntry.Info().Message("Passed.");
                                break;

                            case false:
                                logEntry.Error().Message("Failed.");

                                foreach (var alert in testConfig.GetAlerts(test.Alerts)) alert.Publish(new TestContext
                                {
                                    DataSource = dataSource,
                                    Data = data,
                                    Test = test
                                }, locals);

                                if (!test.CanContinue) return;
                                break;

                            default:
                                logEntry.Warn().Message("Inconclusive");
                                break;
                        }
                    }
                }
            }
        }

        private bool? Assert(DataTable data, TestProperties test, IConstantResolver constants)
        {
            try
            {
                return data.Compute(test.Expression, test.Filter) is bool testResult && testResult == test.Assert;
            }
            catch (Exception ex)
            {
                LogEntry
                    .New()
                    .Error()
                    .Exception(ex)
                    .Message("The expression requires an aggregate function.")
                    .SetValue(nameof(TestProperties.Name), test.Name)
                    .SetValue(nameof(TestProperties.Expression), test.Expression)
                    .SetValue(Globals.Test.FileName, constants.Resolve(Globals.Test.FileName.ToFormatString()))
                    .Log(_logger);
                return null;
            }
        }
    }
}
