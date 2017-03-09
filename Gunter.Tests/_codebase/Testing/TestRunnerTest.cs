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
using Gunter.Alerting;

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

        private TestProperties[] _tests = new TestProperties[]
        {
            new TestProperties
            {
                Name = "Data-source must not be empty.",
                Enabled = true,
                Severity = Severity.Warning,
                Message = "Data-source is empty.",
                DataSources = new [] { 1 },
                Filter = null,
                Assert = false,
                Expression = "COUNT([Id]) = 0",
                CanContinue = false,
                Alerts = { 1 }
            },
            new TestProperties
            {
                Name = "This test is disabled.",
                Enabled = false,
                Severity = Severity.Critical,
                Message = "This test shouldn't have been run.",
                DataSources = new [] { 2 },
                Filter = null,
                Assert = false,
                Expression = "COUNT([Id]) > 0",
                CanContinue = false,
                Alerts = { 1 }
            },
            new TestProperties
            {
                Name = "Debug logging should be disabled.",
                Enabled = true,
                Severity = Severity.Warning,
                Message = "Debug logging is enabled.",
                DataSources = new [] { 2 },
                Filter = "[LogLevel] IN ('debug')",
                Assert = true,
                Expression = "COUNT([Id]) = 0",
                CanContinue = true,
                Alerts = { 1 }
            },
            new TestProperties
            {
                Name = "Elasped seconds can be aggregated.",
                Enabled = true,
                Severity = Severity.Warning,
                Message = "Elapsed seconds not aggregated.",
                DataSources = new [] { 2 },
                Filter = "[ElapsedSeconds] NOT NULL",
                Assert = false,
                Expression = "SUM([ElapsedSeconds]) = 7.0",
                CanContinue = true,
                Alerts = { 1 }
            },
            new TestProperties
            {
                Name = "There should not be any errors.",
                Enabled = true,
                Severity = Severity.Critical,
                Message = "Errors found.",
                DataSources = new [] { 2 },
                Filter = "[LogLevel] IN ('error')",
                Assert = true,
                Expression = "COUNT([Id]) = 0",
                CanContinue = false,
                Alerts = { 1 }
            },
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
                        new Gunter.Data.Sections.DataSourceSummary(),
                        new Gunter.Data.Sections.ExceptionSummary
                        {
                        
                        },
                    }
                }
            };
        }

        [TestMethod]
        public void RunTests_EmptyDataSource_Fails()
        {
            var testConfig = new Gunter.Testing.TestConfiguration
            {
                DataSources = _dataSources,
                Tests = { _tests[0] },
                Alerts = _alerts
            };

            var testRunner = new TestRunner(new NullLogger());
            testRunner.RunTests(testConfig, ConstantResolver.Empty);

            Assert.AreEqual(1, (_alerts[0] as TestAlert).Messages.Count, "Test should trigger one alert.");
        }

        [TestMethod]
        public void RunTests_LogWithoutErrors_Passes()
        {
            var testConfig = new Gunter.Testing.TestConfiguration
            {
                DataSources = _dataSources,
                Tests = { _tests[0] },
                Alerts = _alerts
            };

            var testRunner = new TestRunner(new NullLogger());
            testRunner.RunTests(testConfig, ConstantResolver.Empty);

            Assert.AreEqual(0, (_alerts[0] as TestAlert).Messages.Count, "Test should not publish results.");
        }

        [TestMethod]
        public void RunTests_LogWithErrors_OneAlert()
        {
            var testConfig = new Gunter.Testing.TestConfiguration
            {
                DataSources =
                {
                    new TestDataSource(1)
                    {
                        { "TEST", "info", 1f, "Msg1", null },
                        { "TEST", "error", 1f, "Msg2", "This one went wrong." },
                        { "TEST", "info", 1f, "Msg3", null },
                    }
                },
                Tests =
                {
                    new Gunter.Testing.TestProperties
                    {
                        Enabled = true,
                        Severity = Severity.Critical,
                        Message = "Data-source is not empty.",
                        DataSources = new [] { 1 },
                        Filter = "[LogLevel] IN ('error')",
                        Assert = false,
                        Expression = "COUNT([Id]) > 0",
                        CanContinue = false,
                        //Publish = AlertWhen.Failed
                    }
                },
                Alerts = _alerts
            };

            var testRunner = new TestRunner(new NullLogger());
            testRunner.RunTests(testConfig, ConstantResolver.Empty);

            Assert.AreEqual(1, (_alerts[0] as TestAlert).Messages.Count, "Test should publish results.");
        }
    }
}
