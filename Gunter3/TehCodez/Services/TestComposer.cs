using System.Collections.Generic;
using System.Linq;
using System;
using Gunter.Services;
using System.Data;
using Gunter.Data;
using JetBrains.Annotations;

namespace Gunter.Services
{
    internal interface ITestComposer
    {
        IEnumerable<TestUnit> ComposeTests(TestFile testFile);
    }

    [UsedImplicitly]
    internal class TestComposer : ITestComposer
    {
        public IEnumerable<TestUnit> ComposeTests(TestFile testFile)
        {
            var count = 1;
            var testUnits =
                from testCase in testFile.Tests
                let dataSources =
                    (from id in testCase.DataSourceIds
                     join ds in testFile.DataSources on id equals ds.Id
                     select ds).Distinct().ToList()
                from dataSource in dataSources
                let alerts =
                    (from id in testCase.AlertIds
                     join alert in testFile.Alerts on id equals alert.Id
                     select alert).Distinct().ToList()
                let reports =
                    (from id in alerts.SelectMany(alert => alert.Reports)
                     join report in testFile.Reports on id equals report.Id
                     select report).Distinct().ToList()
                select new TestUnit
                {
                    TestFile = testFile,
                    TestCase = testCase,
                    TestNumber = count++,
                    DataSource = dataSource,
                    Alerts = alerts,
                    Reports = reports,
                };

            return testUnits;

            TestCase PopulateTestCase(TestCase testCase)
            {
                return testCase;
            }
        }
    }
}
