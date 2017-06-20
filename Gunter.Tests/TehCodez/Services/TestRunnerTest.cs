using System;
using System.Collections.Generic;
using System.Linq;
using Gunter.Data;
using Gunter.Messaging;
using Gunter.Reporting;
using Gunter.Reporting.Data;
using Gunter.Reporting.Modules;
using Gunter.Services;
using Gunter.Tests.Data.Fakes;
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

        private readonly List<TestAlert> _alerts = new List<TestAlert>
        {
            new TestAlert { Id = 1 }
        };

        private readonly TestRunner _testRunner = new TestRunner(new NullLogger(), new VariableBuilder());

        [TestInitialize]
        public void TestInitialize()
        {
            var testFile = new TestFile
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
                    }
                },
                Alerts =
                {
                    new TestAlert { Id = 1 }                     
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
                                        Name = "_nvarchar",
                                        IsKey = true,
                                        Filter = new Gunter.Reporting.Filters.FirstLine(),
                                        Total = ColumnTotal.First
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
            foreach (var alert in _alerts) alert.Dispose();
        }

        [TestMethod]
        public void RunTests_TestCaseDisabled_TestNotRun()
        {
            var testFile = new TestFile
            {
                DataSources = _dataSources,
                Tests =
                {
                    new TestCase
                    {
                        Enabled = false,
                        Severity = TestSeverity.Fatal,
                        Message = "This test should not run.",
                        //DataSources = { 2 },
                        Filter = null,
                        Expression = null,
                        Assert = false,
                        OnFailed = TestResultActions.Alert | TestResultActions.Halt,
                        Alerts = { 1 }
                    }
                },
                Alerts = _alerts.Cast<IAlert>().ToList()
            };

            _testRunner.RunTestFiles(new[] { testFile }, new string[0], VariableResolver.Empty);
            Assert.AreEqual(0, _alerts.ElementAt(0).PublishedReports.Count);
        }

        [TestMethod]
        public void RunTests_TestCaseSeverityInfo_Success()
        {
            _alert = new TestAlert { Id = 1, Reports = { 1 } };

            var testConfig = new TestFile
            {
                DataSources = _dataSources,
                Tests =
                {
                    new TestCase
                    {
                        Enabled = true,
                        Severity = TestSeverity.Info,
                        Message = "Service runs smoothly.",
                        DataSources = { 2 },
                        Filter = "[LogLevel] = 'info' AND [Environment] = 'test'",
                        Expression = "COUNT([LogLevel]) = 2",
                        Assert = true,
                        OnFailed = TestResultActions.Alert | TestResultActions.Halt,
                        Alerts = { 1 }
                    }
                },
                Alerts = { _alert },
                Reports =
                {
                    new Report
                    {
                        Id = 1,
                        Modules =
                        {
                            //new Module
                            //{
                            //    //Detail = new TestCaseInfo()
                            //},
                            //new Module
                            //{
                            //    //Detail = new DataSourceInfo()
                            //},
                            //new Module
                            //{
                            //    //Detail = new DataSummary()
                            //}
                        }
                    }
                }
            };

            _testRunner.RunTestFiles(new[] { testConfig }, VariableResolver.Empty);
            Assert.AreEqual(1, _alert.PublishedReports.Count);
            var report = _alert.PublishedReports.Single();

            var testCaseInfo = report.Sections.ElementAt(0).Detail.Tables[nameof(TestCaseInfo)];
            Assert.AreEqual(7, testCaseInfo.Rows.Count);
            Assert.AreEqual("Info", testCaseInfo.Rows[0]["Value"]);
            Assert.AreEqual("Info", testCaseInfo.Rows[0]["Value"]);
        }

        [TestMethod]
        public void RunTests_TestCaseWithAssertFalseEvaluatesToTrue_TestFails()
        {
            // This test verifies that a data-source is not empty but it is so it fails.

            var testConfig = new Gunter.Data.TestFile
            {
                DataSources = _dataSources,
                Tests =
                {
                    new TestCase
                    {
                        Enabled = true,
                        Severity = TestSeverity.Warn,
                        Message = "Data-source must be empty.",
                        DataSources = { 1 },
                        Filter = null,
                        Expression = "COUNT([Id]) = 0",
                        Assert = false,
                        OnFailed = TestResultActions.Alert | TestResultActions.Halt,
                        Alerts = { 1 }
                    }
                },
                Alerts = _alerts
            };

            _testRunner.RunTestFiles(new[] { testConfig }, VariableResolver.Empty);
            var mockAlert = _alerts.ElementAtOrDefault(0) as TestAlert;
            Assert.IsNotNull(mockAlert);
            Assert.AreEqual(1, mockAlert.PublishedReports.Count);
        }

        [TestMethod]
        public void RunTests_TestCaseWithBreakOnFailureFalse_ExecutionContinues()
        {
            // This test verfies with two identical tests that the execution breaks as soon as the first test fails.

            var testConfig = new Gunter.Data.TestFile
            {
                DataSources = _dataSources,
                Tests =
                {
                    new TestCase
                    {
                        Enabled = true,
                        Severity = TestSeverity.Warn,
                        Message = "Data-source must be empty.",
                        DataSources = { 2 },
                        Filter = null,
                        Assert = true,
                        Expression = "COUNT([Id]) = 0",
                        OnFailed = TestResultActions.Alert | TestResultActions.Halt,
                        Alerts = { 1 }
                    },
                    new TestCase
                    {
                        Enabled = true,
                        Severity = TestSeverity.Warn,
                        Message = "Data-source must be empty.",
                        DataSources = { 2 },
                        Filter = null,
                        Assert = true,
                        Expression = "COUNT([Id]) = 0",
                        OnFailed = TestResultActions.Alert | TestResultActions.Halt,
                        Alerts = { 1 }
                    }
                },
                Alerts = _alerts
            };

            _testRunner.RunTestFiles(new[] { testConfig }, VariableResolver.Empty);
            var mockAlert = _alerts.ElementAtOrDefault(0) as TestAlert;
            Assert.IsNotNull(mockAlert);
            Assert.AreEqual(2, mockAlert.PublishedReports.Count);
        }

        [TestMethod]
        public void RunTests_BreakOnFailureTrue_ExcutionBreaks()
        {
            // This test verfies with two identical tests that the execution continues even though the first test fails.

            var testConfig = new Gunter.Data.TestFile
            {
                DataSources = _dataSources,
                Tests =
                {
                    new TestCase
                    {
                        Enabled = true,
                        Severity = TestSeverity.Warn,
                        Message = "Data-source must be empty.",
                        DataSources = { 2 },
                        Filter = null,
                        Assert = true,
                        Expression = "COUNT([Id]) = 0",
                        OnFailed = TestResultActions.Alert | TestResultActions.Halt,
                        Alerts = { 1 }
                    },
                    new TestCase
                    {
                        Enabled = true,
                        Severity = TestSeverity.Warn,
                        Message = "Data-source must be empty.",
                        DataSources = { 2 },
                        Filter = null,
                        Assert = true,
                        Expression = "COUNT([Id]) = 0",
                        OnFailed = TestResultActions.Alert | TestResultActions.Halt,
                        Alerts = { 1 }
                    }
                },
                Alerts = _alerts
            };

            _testRunner.RunTestFiles(new[] { testConfig }, VariableResolver.Empty);
            var mockAlert = _alerts.ElementAtOrDefault(0) as TestAlert;
            Assert.IsNotNull(mockAlert);
            Assert.AreEqual(1, mockAlert.PublishedReports.Count);
        }

        [TestMethod]
        public void RunTests_TestCaseWithFilter_TestPasses()
        {
            // This test verfies that only filtered rows are tested.

            var testConfig = new Gunter.Data.TestFile
            {
                DataSources = _dataSources,
                Tests =
                {
                    new TestCase
                    {
                        Enabled = true,
                        Severity = TestSeverity.Warn,
                        Message = "Debug logging is enabled.",
                        DataSources = { 2 },
                        Filter = "[LogLevel] IN ('debug')",
                        Assert = true,
                        Expression = "COUNT([Id]) = 1",
                        OnFailed = TestResultActions.Alert | TestResultActions.Halt,
                        Alerts = { 1 }
                    },
                },
                Alerts = _alerts
            };

            _testRunner.RunTestFiles(new[] { testConfig }, VariableResolver.Empty);
            var mockAlert = _alerts.ElementAtOrDefault(0) as TestAlert;
            Assert.IsNotNull(mockAlert);
            Assert.AreEqual(0, mockAlert.PublishedReports.Count);
        }

        [TestMethod]
        public void RunTests_TestCaseWithInvalidExpression_Inconclusive()
        {
            // This test verfies that only filtered rows are tested.

            var testConfig = new Gunter.Data.TestFile
            {
                DataSources = _dataSources,
                Tests =
                {
                    new TestCase
                    {
                        Enabled = true,
                        Severity = TestSeverity.Warn,
                        Message = "Debug logging is enabled.",
                        DataSources = { 2 },
                        Filter = null,
                        Assert = true,
                        Expression = "[LogLevel] IN ('debug')",
                        OnFailed = TestResultActions.Alert | TestResultActions.Halt,
                        Alerts = { 1 }
                    },
                },
                Alerts = _alerts
            };

            var testRunner = new TestRunner(new NullLogger(), new VariableBuilder());
            testRunner.RunTestFiles(new[] { testConfig }, VariableResolver.Empty);

            Assert.AreEqual(0, (_alerts[0] as TestAlert).PublishedReports.Count);
        }
    }
}
