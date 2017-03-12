using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gunter.Testing;
using Gunter.Tests.Data;
using Gunter.Tests.Alerting;
using Reusable.Logging;
using Gunter.Data;
using Gunter.Services;
using System;
using System.Linq;
using Gunter.Alerts;

namespace Gunter.Tests
{
    [TestClass]
    public class TestRunnerTest
    {
        private List<IDataSource> _dataSources = new List<IDataSource>
        {
            new TestDataSource(1),
            new TestDataSource(2)
            {
                { "test1", "info", 0.0f, "Info message.", null },
                { "test1", "info", 1.0f, "Info maessage.", null },
                { "test1", "debug", null, "Debug message.", null },
                { "test2", "error", 2.0f, "Error message.", new Func<Exception>(() => { return new Exception(); })().ToString() },
                { "test2", "error", 4.0f, "Error message.", new Func<Exception>(() => { return new Exception(); })().ToString() },
            }
        };

        private List<IAlert> _alerts;

        [TestInitialize]
        public void TestInitialize()
        {
            _alerts = new List<IAlert>
            {
                new TestAlert
                {
                    Id = 1,
                    Sections =
                    {
                        new Gunter.Data.Sections.DataSourceInfo(new NullLogger()),
                        new Gunter.Data.Sections.DataAggregate(new NullLogger())
                        {

                        },
                    }
                }
            };
        }

        [TestMethod]
        public void RunTests_TestDisabled_NoAlert()
        {
            var testConfig = new Gunter.Testing.TestConfiguration
            {
                DataSources = _dataSources,
                Tests =
                {
                    new TestProperties
                    {
                        Enabled = false,
                        Severity = Severity.Critical,
                        Message = "This test should not run.",
                        DataSources = { 2 },
                        Filter = null,
                        Expression = "COUNT([Id]) > 0",
                        Assert = false,
                        CanContinue = false,
                        Alerts = { 1 }
                    }
                },
                Alerts = _alerts
            };

            var testRunner = new TestRunner(new NullLogger());
            testRunner.RunTests(testConfig, ConstantResolver.Empty);

            Assert.AreEqual(0, (_alerts[0] as TestAlert).Messages.Count);
        }

        [TestMethod]
        public void RunTests_AssertTrue_Fails()
        {
            // This tes verfies that the a data-source is empty with assert = true.

            var testConfig = new Gunter.Testing.TestConfiguration
            {
                DataSources = _dataSources,
                Tests =
                {
                    new TestProperties
                    {
                        Enabled = true,
                        Severity = Severity.Warning,
                        Message = "Data-source must be empty.",
                        DataSources = { 2 },
                        Filter = null,
                        Assert = true,
                        Expression = "COUNT([Id]) = 0",
                        CanContinue = false,
                        Alerts = { 1 }
                    }
                },
                Alerts = _alerts
            };

            var testRunner = new TestRunner(new NullLogger());
            testRunner.RunTests(testConfig, ConstantResolver.Empty);

            Assert.AreEqual(1, (_alerts[0] as TestAlert).Messages.Count);
        }

        [TestMethod]
        public void RunTests_AssertFalse_Fails()
        {
            // This test verifies that a data-source is not empty by nagating the expression with assert = false.

            var testConfig = new Gunter.Testing.TestConfiguration
            {
                DataSources = _dataSources,
                Tests =
                {
                    new TestProperties
                    {
                        Enabled = true,
                        Severity = Severity.Warning,
                        Message = "Data-source must be empty.",
                        DataSources = { 1 },
                        Filter = null,
                        Expression = "COUNT([Id]) = 0",
                        Assert = false,
                        CanContinue = false,
                        Alerts = { 1 }
                    }
                },
                Alerts = _alerts
            };

            var testRunner = new TestRunner(new NullLogger());
            testRunner.RunTests(testConfig, ConstantResolver.Empty);

            Assert.AreEqual(1, (_alerts[0] as TestAlert).Messages.Count);
        }

        [TestMethod]
        public void RunTests_CanContinueFalse_Breakes()
        {
            // This test verfies that the execution breaks as soon as the first test fails.

            var testConfig = new Gunter.Testing.TestConfiguration
            {
                DataSources = _dataSources,
                Tests =
                {
                    new TestProperties
                    {
                        Enabled = true,
                        Severity = Severity.Warning,
                        Message = "Data-source must be empty.",
                        DataSources = { 2 },
                        Filter = null,
                        Assert = true,
                        Expression = "COUNT([Id]) = 0",
                        CanContinue = false,
                        Alerts = { 1 }
                    },
                    new TestProperties
                    {
                        Enabled = true,
                        Severity = Severity.Warning,
                        Message = "Data-source must be empty.",
                        DataSources = { 2 },
                        Filter = null,
                        Assert = true,
                        Expression = "COUNT([Id]) = 0",
                        CanContinue = false,
                        Alerts = { 1 }
                    }
                },
                Alerts = _alerts
            };

            var testRunner = new TestRunner(new NullLogger());
            testRunner.RunTests(testConfig, ConstantResolver.Empty);

            Assert.AreEqual(1, (_alerts[0] as TestAlert).Messages.Count);
        }

        [TestMethod]
        public void RunTests_CanContinueTrue_Continues()
        {
            // This test verfies that the execution continues even though the first test fails.

            var testConfig = new Gunter.Testing.TestConfiguration
            {
                DataSources = _dataSources,
                Tests =
                {
                    new TestProperties
                    {
                        Enabled = true,
                        Severity = Severity.Warning,
                        Message = "Data-source must be empty.",
                        DataSources = { 2 },
                        Filter = null,
                        Assert = true,
                        Expression = "COUNT([Id]) = 0",
                        CanContinue = true,
                        Alerts = { 1 }
                    },
                    new TestProperties
                    {
                        Enabled = true,
                        Severity = Severity.Warning,
                        Message = "Data-source must be empty.",
                        DataSources = { 2 },
                        Filter = null,
                        Assert = true,
                        Expression = "COUNT([Id]) = 0",
                        CanContinue = false,
                        Alerts = { 1 }
                    }
                },
                Alerts = _alerts
            };

            var testRunner = new TestRunner(new NullLogger());
            testRunner.RunTests(testConfig, ConstantResolver.Empty);

            Assert.AreEqual(2, (_alerts[0] as TestAlert).Messages.Count);
        }

        [TestMethod]
        public void RunTests_Filter_Passes()
        {
            // This test verfies that only filtered rows are tested.

            var testConfig = new Gunter.Testing.TestConfiguration
            {
                DataSources = _dataSources,
                Tests =
                {
                    new TestProperties
                    {
                        Enabled = true,
                        Severity = Severity.Warning,
                        Message = "Debug logging is enabled.",
                        DataSources = { 2 },
                        Filter = "[LogLevel] IN ('debug')",
                        Assert = true,
                        Expression = "COUNT([Id]) = 1",
                        CanContinue = true,
                        Alerts = { 1 }
                    },
                },
                Alerts = _alerts
            };

            var testRunner = new TestRunner(new NullLogger());
            testRunner.RunTests(testConfig, ConstantResolver.Empty);

            Assert.AreEqual(0, (_alerts[0] as TestAlert).Messages.Count);
        }

        [TestMethod]
        public void RunTests_InvalidExpression_Inconclusive()
        {
            // This test verfies that only filtered rows are tested.

            var testConfig = new Gunter.Testing.TestConfiguration
            {
                DataSources = _dataSources,
                Tests =
                {
                    new TestProperties
                    {
                        Enabled = true,
                        Severity = Severity.Warning,
                        Message = "Debug logging is enabled.",
                        DataSources = { 2 },
                        Filter = null,
                        Assert = true,
                        Expression = "[LogLevel] IN ('debug')",
                        CanContinue = true,
                        Alerts = { 1 }
                    },
                },
                Alerts = _alerts
            };

            var testRunner = new TestRunner(new NullLogger());
            testRunner.RunTests(testConfig, ConstantResolver.Empty);

            Assert.AreEqual(0, (_alerts[0] as TestAlert).Messages.Count);
        }
    }
}
