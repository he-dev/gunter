using System;
using System.Collections.Generic;
using System.Linq;
using Gunter.Data;
using Gunter.Messaging;
using Gunter.Reporting;
using Gunter.Reporting.Data;
using Gunter.Reporting.Modules;
using Gunter.Services;
using Gunter.Tests.Data;
using Gunter.Tests.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reusable.Logging;

namespace Gunter.Tests.Services
{
    [TestClass]
    public class TestRunnerTest
    {
        private readonly List<IDataSource> _dataSources = new List<IDataSource>
        {
            new TestDataSource(1),
            new TestDataSource(2)
                .AddRow(row => row._string("foo")._int(1)._double(2.2)._decimal(3.5m)._datetime(new DateTime(2017, 5, 1))._bool(true))
                .AddRow(row => row._string("foo")._int(3)._double(null)._decimal(5m)._datetime(new DateTime(2017, 5, 2))._bool(true))
                .AddRow(row => row._string("bar")._int(8)._double(null)._decimal(1m)._datetime(null)._bool(false))
                .AddRow(row => row._string("bar")._int(null)._double(3.5)._decimal(null)._datetime(new DateTime(2017, 5, 3))._bool(false))
                .AddRow(row => row._string("bar")._int(4)._double(7)._decimal(2m)._datetime(new DateTime(2017, 5, 4))._bool(null))
                .AddRow(row => row._string(null)._int(null)._double(2.5)._decimal(8m)._datetime(new DateTime(2017, 5, 5))._bool(true))
                .AddRow(row => row._string("")._int(2)._double(6)._decimal(1.5m)._datetime(new DateTime(2017, 5, 6))._bool(null))
        };        

        private TestFile TestFile { get; set; }

        private readonly TestRunner _testRunner = new TestRunner(new NullLogger(), new VariableBuilder());

        [TestInitialize]
        public void TestInitialize()
        {
            TestFile = new TestFile
            {
                DataSources = _dataSources,
                Tests =
                {
                    new TestCase
                    {
                        Enabled = false,
                        Severity = TestSeverity.Debug,
                        Message = "Empty test.",
                        DataSources = { 1, 2 },
                        Filter = null,
                        Expression = null,
                        Assert = false,
                        OnPassed = TestResultActions.None,
                        OnFailed = TestResultActions.None,
                        Alerts = { 1 }
                    },
                    new TestCase
                    {
                        Enabled = false,
                        Severity = TestSeverity.Info,
                        Message = "Only foos.",
                        DataSources = { 2 },
                        Filter = "[_string] = 'foo'",
                        Expression = "Count([_string]) = 2",
                        Assert = true,
                        OnPassed = TestResultActions.Alert,
                        OnFailed = TestResultActions.None,
                        Alerts = { 1 }
                    }
                },
                Alerts =
                {
                    new TestAlert { Id = 1, Reports = { 1 }}
                },
                Reports =
                {
                    new Report
                    {
                        Id = 1,
                        Title = "Test report",
                        Modules =
                        {
                            new Greeting
                            {
                                Heading = "Hallo test!",
                                Text = "{TestCase.Message}"
                            },
                            new TestCaseInfo
                            {
                                Heading = "Test case"
                            },
                            new DataSourceInfo
                            {
                                Heading = "Data source"
                            },
                            new DataSummary
                            {
                                Heading = "Data summary",
                                Columns =
                                {
                                    new ColumnOption
                                    {
                                        Name = "_string",
                                        IsKey = true,
                                        Filter = new Gunter.Reporting.Filters.FirstLine(),
                                        Total = ColumnTotal.First
                                    },
                                    new ColumnOption
                                    {
                                        Name = "_int",
                                        IsKey = false,
                                        Filter = new Gunter.Reporting.Filters.Unchanged(),
                                        Total = ColumnTotal.Sum
                                    },
                                    new ColumnOption
                                    {
                                        Name = "_double",
                                        IsKey = false,
                                        Filter = new Gunter.Reporting.Filters.Unchanged(),
                                        Total = ColumnTotal.Average
                                    },
                                    new ColumnOption
                                    {
                                        Name = "_decimal",
                                        IsKey = false,
                                        Filter = new Gunter.Reporting.Filters.Unchanged(),
                                        Total = ColumnTotal.Sum
                                    },
                                    new ColumnOption
                                    {
                                        Name = "_datetime",
                                        IsKey = false,
                                        Filter = new Gunter.Reporting.Filters.Unchanged(),
                                        Total = ColumnTotal.Max
                                    },
                                    new ColumnOption
                                    {
                                        Name = "_bool",
                                        IsKey = false,
                                        Filter = new Gunter.Reporting.Filters.Unchanged(),
                                        Total = ColumnTotal.Count
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }
        [TestCleanup]
        public void TestCleanup()
        {
            foreach (var alert in TestFile.Alerts.Cast<TestAlert>()) alert.Dispose();
        }

        [TestMethod]
        public void RunTests_AllTestsDisabled_TestNotRun()
        {
            _testRunner.RunTestFiles(new[] { TestFile }, new string[0], VariableResolver.Empty);
            //Assert.AreEqual(0, TestFile.Alerts.Cast<TestAlert>().ElementAt(0).PublishedReports.Count);
        }

        [TestMethod]
        public void RunTests_SeverityInfo_Alert()
        {
            TestFile.Tests[1].Enabled = true;
            _testRunner.RunTestFiles(new[] { TestFile }, new string[0], VariableResolver.Empty);
            //Assert.AreEqual(1, TestFile.Alerts.Cast<TestAlert>().ElementAt(0).PublishedReports.Count);
        }
    }
}
