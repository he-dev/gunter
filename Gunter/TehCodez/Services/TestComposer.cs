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
        public static IEnumerable<TestUnit> ComposeTests(TestFile testFile, IVariableResolver globalVariables)
        {
            var testUnits =
                from test in testFile.Tests
                let localVariables = globalVariables.MergeWith(testFile.Locals)
                let dataSources =
                    (from id in test.DataSources
                     join ds in testFile.DataSources on id equals ds.Id
                     select ds).Distinct().ToList()
                from dataSource in dataSources
                let alerts =
                    (from id in test.Alerts
                     join alert in testFile.Alerts on id equals alert.Id
                     select alert).Distinct().ToList()
                let reports =
                    (from id in alerts.SelectMany(alert => alert.Reports)
                     join report in testFile.Reports on id equals report.Id
                     select report).Distinct().ToList()
                select new TestUnit
                {
                    //FileName = testFile.FileName,
                    Test = test.UpdateVariables(localVariables),
                    DataSource = dataSource.UpdateVariables(localVariables),
                    Alerts = alerts,
                    Reports = reports,
                };

            return testUnits;
        }
    }
}
