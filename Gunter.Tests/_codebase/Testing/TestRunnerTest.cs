using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gunter.Tests.Data;
using Gunter.Tests.Alerting;
using Reusable.Logging;
using Gunter.Data;
using Gunter.Services;
using System;
using System.Linq;
using Gunter.Messaging;
using Gunter.Reporting.Tables;

namespace Gunter.Tests
{
    [TestClass]
    public class TestRunnerTest
    {
        private List<IDataSource> _dataSources = new List<IDataSource>
        {
            new MockDataSource(1),
            new MockDataSource(2)
            {
                { "debug", "info", 0.0f, "Info message ABC.", null },
                { "debug", "info", 1.0f, "Info maessage ABC.", null },
                { "debug", "debug", null, "Debug message JKL.", null },
                { "release", "error", 2.0f, "Error message ABC.", new Func<Exception>(() => { return new ArgumentException(); })().ToString() },
                { "release", "error", 4.0f, "Error message XYZ.", new Func<Exception>(() => { return new DivideByZeroException(); })().ToString() },
                { "release", "error", 3.0f, "Error message ABC.", null },
                { "release", "error", 3.0f, "Error message ABC.", new Func<Exception>(() => { return new ArgumentException(); })().ToString() },
            }
        };

        private List<IAlert> _alerts;

        private readonly TestRunner _testRunner = new TestRunner(new NullLogger());

        [TestInitialize]
        public void TestInitialize()
        {
            _alerts = new List<IAlert>
            {
                new MockAlert
                {
                    Id = 1,
                    Sections =
                    {
                        new Gunter.Alerts.Sections.Text(new NullLogger()),
                        new DataSourceInfo(new NullLogger()),
                        new DataSummary(new NullLogger())
                        {

                        },
                    }
                }
            };
        }

        [TestMethod]
        public void RunTests_TestCaseDisabled_TestNotRun()
        {
            var testConfig = new Gunter.Data.TestCollection
            {
                DataSources = _dataSources,
                Tests =
                {
                    new TestCase
                    {
                        Enabled = false,
                        Severity = TestSeverity.Critical,
                        Message = "This test should not run.",
                        DataSources = { 2 },
                        Filter = null,
                        Expression = "COUNT([Id]) > 0",
                        Assert = false,
                        ContinueOnFailure = false,
                        Alerts = { 1 }
                    }
                },
                Alerts = _alerts
            };

            _testRunner.RunTests(new[] { testConfig }, ConstantResolver.Empty);
            var mockAlert = _alerts.ElementAtOrDefault(0) as MockAlert;
            Assert.IsNotNull(mockAlert);
            Assert.AreEqual(0, mockAlert.Data.Count);
        }

        [TestMethod]
        public void RunTests_TestCaseWithAssertTrueEvaluatesToFalse_TestFails()
        {
            // This test verifies that the data-source is empty but it isn't so it fails.

            var testConfig = new Gunter.Data.TestCollection
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
                    }
                },
                Alerts = _alerts
            };

            _testRunner.RunTests(new[] { testConfig }, ConstantResolver.Empty);
            var mockAlert = _alerts.ElementAtOrDefault(0) as MockAlert;
            Assert.IsNotNull(mockAlert);
            Assert.AreEqual(1, mockAlert.Data.Count);
        }

        [TestMethod]
        public void RunTests_TestCaseWithAssertFalseEvaluatesToTrue_TestFails()
        {
            // This test verifies that a data-source is not empty but it is so it fails.

            var testConfig = new Gunter.Data.TestCollection
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

            _testRunner.RunTests(new[] { testConfig }, ConstantResolver.Empty);
            var mockAlert = _alerts.ElementAtOrDefault(0) as MockAlert;
            Assert.IsNotNull(mockAlert);
            Assert.AreEqual(1, mockAlert.Data.Count);
        }

        [TestMethod]
        public void RunTests_TestCaseWithBreakOnFailureFalse_ExecutionContinues()
        {
            // This test verfies with two identical tests that the execution breaks as soon as the first test fails.

            var testConfig = new Gunter.Data.TestCollection
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

            _testRunner.RunTests(new[] { testConfig }, ConstantResolver.Empty);
            var mockAlert = _alerts.ElementAtOrDefault(0) as MockAlert;
            Assert.IsNotNull(mockAlert);
            Assert.AreEqual(2, mockAlert.Data.Count);
        }

        [TestMethod]
        public void RunTests_BreakOnFailureTrue_ExcutionBreaks()
        {
            // This test verfies with two identical tests that the execution continues even though the first test fails.

            var testConfig = new Gunter.Data.TestCollection
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

            _testRunner.RunTests(new[] { testConfig }, ConstantResolver.Empty);
            var mockAlert = _alerts.ElementAtOrDefault(0) as MockAlert;
            Assert.IsNotNull(mockAlert);
            Assert.AreEqual(1, mockAlert.Data.Count);
        }

        [TestMethod]
        public void RunTests_TestCaseWithFilter_TestPasses()
        {
            // This test verfies that only filtered rows are tested.

            var testConfig = new Gunter.Data.TestCollection
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

            _testRunner.RunTests(new[] { testConfig }, ConstantResolver.Empty);
            var mockAlert = _alerts.ElementAtOrDefault(0) as MockAlert;
            Assert.IsNotNull(mockAlert);
            Assert.AreEqual(0, mockAlert.Data.Count);
        }

        [TestMethod]
        public void RunTests_TestCaseWithInvalidExpression_Inconclusive()
        {
            // This test verfies that only filtered rows are tested.

            var testConfig = new Gunter.Data.TestCollection
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
            testRunner.RunTests(new[] { testConfig }, ConstantResolver.Empty);

            Assert.AreEqual(0, (_alerts[0] as MockAlert).Data.Count);
        }
    }
}
