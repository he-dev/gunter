using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Linq;
using Gunter.Alerting;
using Gunter.Reporting;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable.Extensions;

namespace Gunter.Data
{
    [PublicAPI]
    public class TestCase
    {
        [DefaultValue(true)]
        public bool Enabled { get; set; }

        [DefaultValue(TestSeverity.Warn)]
        public TestSeverity Severity { get; set; }

        [JsonRequired]
        public string Message { get; set; }

        [JsonRequired]
        [JsonProperty("DataSources")]
        public List<int> DataSourceIds { get; set; } = new List<int>();

        public string Filter { get; set; }

        [JsonRequired]
        public string Expression { get; set; }

        [DefaultValue(true)]
        public bool Assert { get; set; }

        [DefaultValue(TestResultActions.None)]
        public TestResultActions OnPassed { get; set; }

        [DefaultValue(TestResultActions.Alert | TestResultActions.Halt)]
        public TestResultActions OnFailed { get; set; }

        [JsonProperty("Alerts")]
        public List<int> AlertIds { get; set; } = new List<int>();

        [JsonProperty("Profiles")]
        public List<string> Profiles { get; set; } = new List<string>();
    }

    public static class TestCaseExtensions
    {
        public static IEnumerable<IDataSource> DataSources(this TestCase testCase, TestFile testFile)
        {
            return
                (from id in testCase.DataSourceIds
                 join ds in testFile.DataSources on id equals ds.Id
                 select ds).Distinct();
        }

        public static IEnumerable<IAlert> Alerts(this TestCase testCase, TestFile testFile)
        {
            return
                (from id in testCase.AlertIds
                 join alert in testFile.Alerts on id equals alert.Id
                 select alert).Distinct();
        }

        public static IEnumerable<IReport> Reports(this TestCase testCase, TestFile testFile)
        {
            return
                (from id in testCase.Alerts(testFile).SelectMany(alert => alert.ReportIds)
                 join report in testFile.Reports on id equals report.Id
                 select report).Distinct();
        }

        public static bool CanExecute(this TestCase testCase, IEnumerable<string> profiles)
        {
            
            // In order for a test to be runnable it has to be enabled and its profile needs to match the list or the list needs to be empty.

            return
                testCase.Enabled &&
                ProfileMatches();

            bool ProfileMatches()
            {
                return
                    profiles.Empty() ||
                    profiles.Any(runnableProfile => profiles.Contains(runnableProfile, StringComparer.OrdinalIgnoreCase));
            }
        }
    }
}

