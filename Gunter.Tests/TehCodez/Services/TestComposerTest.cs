using System;
using System.Linq;
using Gunter.Data;
using Gunter.Reporting;
using Gunter.Services;
using Gunter.Tests.Data.Fakes;
using Gunter.Tests.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gunter.Tests.Services
{
    [TestClass]
    public class TestComposerTest
    {
        [TestMethod]
        public void ComposeTests_AllOptions_TestUnits()
        {
            var testFile = new TestFile
            {
                DataSources = { new TestDataSource(2) },
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
                        OnPassed = TestResultActions.Halt | TestResultActions.Alert,
                        Alerts = { 1 }
                    }
                },
                Alerts = { new TestAlert { Id = 1, Reports = { 2 }} },
                Reports = { new Report { Id = 2 } }
            };

            var tuple = TestComposer.ComposeTests(testFile, VariableResolver.Empty).ToList();
            Assert.AreEqual(1, tuple.Count);
            Assert.AreEqual(2, tuple.Single().DataSource.Id);
            Assert.AreEqual(1, tuple.Single().Alerts.Count());
            Assert.AreEqual(1, tuple.Single().Reports.Count());
        }
    }
}
