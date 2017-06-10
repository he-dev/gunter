using System.Collections.Generic;
using System.Linq;
using System;
using Gunter.Services;
using System.Data;
using Gunter.Data;

namespace Gunter.Services
{
    internal static class TestComposer
    {
        public static IEnumerable<TestConfiguration> ComposeTests(TestFile config, IConstantResolver constants)
        {
            var profileExists = constants.TryGetValue(VariableName.TestCase.Profile, out object profile);
            var results =
                from test in config.Tests
                where test.Enabled && (!profileExists || test.Profiles.Contains((string)profile, StringComparer.OrdinalIgnoreCase))
                let dataSources =
                    (from id in test.DataSources
                     join ds in config.DataSources on id equals ds.Id
                     select ds).Distinct().ToList()
                let alerts =
                    (from id in test.Alerts
                     join alert in config.Alerts on id equals alert.Id
                     select alert).Distinct().ToList()
                let reports =
                    (from id in alerts.SelectMany(alert => alert.Reports)
                     join report in config.Reports on id equals report.Id
                     select report).Distinct().ToList()
                select new TestConfiguration
                {
                    Test = test,
                    DataSources = dataSources,
                    Alerts = alerts,
                    Reports = reports,
                    Constants =
                        constants
                            .UnionWith(config.Locals)
                            .Add(VariableName.TestCollection.FileName, config.FileName)
                            .Add(VariableName.TestCase.Severity, test.Severity)
                            .Add(VariableName.TestCase.Message, test.Message)
                };

            return results;
        }
    }
}
