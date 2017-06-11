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
        public void ComposeTests_abc()
        {
            var testFile = new TestFile
            {
                DataSources = { new FakeDataSource(2) },
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
                        Alerts = { 1 }
                    }
                },
                Alerts = { new TestAlert { Id = 1, Reports = { 2 }} },
                Reports = { new Report { Id = 2 } }
            };

            var tests = TestComposer.ComposeTests(testFile).ToList();
            Assert.AreEqual(1, tests.Count);
            Assert.AreEqual(1, tests.Single().DataSources.Count());
            Assert.AreEqual(1, tests.Single().Alerts.Count());
            Assert.AreEqual(1, tests.Single().Reports.Count());
        }
    }
}
