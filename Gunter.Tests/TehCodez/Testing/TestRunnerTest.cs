using System;
using System.Collections.Generic;
using System.Linq;
using Gunter.Data;
using Gunter.Messaging;
using Gunter.Reporting;
using Gunter.Reporting.Details;
using Gunter.Services;
using Gunter.Tests.Data.Fakes;
using Gunter.Tests.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reusable.Logging;

namespace Gunter.Tests.Testing
{
    [TestClass]
    public class TestRunnerTest
    {
        private readonly List<IDataSource> _dataSources = new List<IDataSource>
        {
            new FakeDataSource(1),
            new FakeDataSource(2)
                .AddRow(row => row.Timestamp(DateTime.UtcNow).Environment("test").LogLevel("info").ElapsedSeconds(0f).Message("foo").Exception(null).ItemCount(1).Completed(true))
                .AddRow(row => row.Timestamp(DateTime.UtcNow).Environment("test").LogLevel("info").ElapsedSeconds(2f).Message("foo").Exception(null).ItemCount(3).Completed(true))
                .AddRow(row => row.Timestamp(DateTime.UtcNow).Environment("test").LogLevel("debug").ElapsedSeconds(3f).Message("bar").Exception(null).ItemCount(10).Completed(true))
                .AddRow(row => row.Timestamp(DateTime.UtcNow).Environment("test").LogLevel("error").ElapsedSeconds(40f).Message("baz").Exception(new DivideByZeroException().ToString()).ItemCount(50).Completed(false))
                .AddRow(row => row.Timestamp(DateTime.UtcNow).Environment("test").LogLevel("error").ElapsedSeconds(10f).Message("qux").Exception(new DivideByZeroException().ToString()).ItemCount(50).Completed(false))
                .AddRow(row => row.Timestamp(DateTime.UtcNow).Environment("test").LogLevel("error").ElapsedSeconds(0f).Message("qux").Exception(new InvalidOperationException().ToString()).ItemCount(5).Completed(false))
                .AddRow(row => row.Timestamp(DateTime.UtcNow).Environment("prod").LogLevel("info").ElapsedSeconds(5f).Message("foo").Exception(null).ItemCount(8).Completed(true))            
        };

        private List<IAlert> _alerts;

        private TestAlert _alert;

        private readonly TestRunner _testRunner = new TestRunner(new NullLogger());

        [TestCleanup]
        public void TestCleanup()
        {
            _alert.Dispose();
        }

        [TestMethod]
        public void RunTests_TestCaseDisabled_TestNotRun()
        {
            _alert = new TestAlert();
            var testConfig = new TestFile
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
                        ContinueOnFailure = false,
                        Alerts = { 1 }
                    }
                },
                Alerts = { _alert }
            };

            _testRunner.RunTestFiles(new[] { testConfig }, VariableResolver.Empty);
            Assert.AreEqual(0, _alert.PublishedReports.Count);
        }

        [TestMethod]
        public void RunTests_TestCaseSeverityInfo_Success()
        {
            _alert = new TestAlert { Id = 1, Reports = { 1 }};

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
                        ContinueOnFailure = false,
                        AlertTrigger = AlertTrigger.Success,
                        Alerts = { 1 }
                    }
                },
                Alerts = { _alert },
                Reports =
                {
                    new Report
                    {
                        Id = 1,
                        Sections =
                        {
                            new Section
                            {
                                Detail = new TestCaseInfo()
                            },
                            new Section
                            {
                                Detail = new DataSourceInfo()
                            },
                            new Section
                            {
                                //Detail = new DataSummary()
                            }
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
                        Severity = TestSeverity.Warning,
                        Message = "Data-source must be empty.",
                        DataSources = { 1 },
                        Filter = null,
                        Expression = "COUNT([Id]) = 0",
                        Assert = false,
                        ContinueOnFailure = false,
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
                        Severity = TestSeverity.Warning,
                        Message = "Data-source must be empty.",
                        DataSources = { 2 },
                        Filter = null,
                        Assert = true,
                        Expression = "COUNT([Id]) = 0",
                        ContinueOnFailure = false,
                        Alerts = { 1 }
                    },
                    new TestCase
                    {
                        Enabled = true,
                        Severity = TestSeverity.Warning,
                        Message = "Data-source must be empty.",
                        DataSources = { 2 },
                        Filter = null,
                        Assert = true,
                        Expression = "COUNT([Id]) = 0",
                        ContinueOnFailure = false,
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
                        Severity = TestSeverity.Warning,
                        Message = "Data-source must be empty.",
                        DataSources = { 2 },
                        Filter = null,
                        Assert = true,
                        Expression = "COUNT([Id]) = 0",
                        ContinueOnFailure = true,
                        Alerts = { 1 }
                    },
                    new TestCase
                    {
                        Enabled = true,
                        Severity = TestSeverity.Warning,
                        Message = "Data-source must be empty.",
                        DataSources = { 2 },
                        Filter = null,
                        Assert = true,
                        Expression = "COUNT([Id]) = 0",
                        ContinueOnFailure = false,
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
                        Severity = TestSeverity.Warning,
                        Message = "Debug logging is enabled.",
                        DataSources = { 2 },
                        Filter = "[LogLevel] IN ('debug')",
                        Assert = true,
                        Expression = "COUNT([Id]) = 1",
                        ContinueOnFailure = true,
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
                        Severity = TestSeverity.Warning,
                        Message = "Debug logging is enabled.",
                        DataSources = { 2 },
                        Filter = null,
                        Assert = true,
                        Expression = "[LogLevel] IN ('debug')",
                        ContinueOnFailure = true,
                        Alerts = { 1 }
                    },
                },
                Alerts = _alerts
            };

            var testRunner = new TestRunner(new NullLogger());
            testRunner.RunTestFiles(new[] { testConfig }, VariableResolver.Empty);

            Assert.AreEqual(0, (_alerts[0] as TestAlert).PublishedReports.Count);
        }
    }
}
