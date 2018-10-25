using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Custom;
using Gunter.Annotations;
using Gunter.Messaging;
using Gunter.Reporting;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable;
using Reusable.Extensions;
using Reusable.OmniLog;

namespace Gunter.Data
{
    [PublicAPI]
    public class TestCase : IMergable
    {
        private readonly Factory _factory;

        public delegate TestCase Factory();

        public TestCase(Factory factory)
        {
            Debug.Assert(factory.IsNotNull());
            _factory = factory;
        }

        public int Id { get; set; }

        public Merge Merge { get; set; }

        [DefaultValue(true)]
        [Mergable]
        public bool Enabled { get; set; }

        [Mergable]
        public LogLevel Level { get; set; } = LogLevel.Warning;

        [Mergable]
        public string Message { get; set; }
        
        [JsonProperty("DataSources", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [Mergable]
        public IList<int> DataSourceIds { get; set; } = new List<int>();

        [Mergable]
        public string Filter { get; set; }

        [Mergable]
        public string Expression { get; set; }

        [DefaultValue(true)]
        [Mergable]
        public bool Assert { get; set; }

        [DefaultValue(TestRunnerActions.None)]
        [Mergable]
        public TestRunnerActions OnPassed { get; set; }

        [DefaultValue(TestRunnerActions.Alert | TestRunnerActions.Halt)]
        [Mergable]
        public TestRunnerActions OnFailed { get; set; }

        [JsonProperty("Messages", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [Mergable]
        public IList<int> MessageIds { get; set; } = new List<int>();

        [JsonProperty("Profiles", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [Mergable]
        public IList<SoftString> Profiles { get; set; } = new List<SoftString>();

        public IMergable New()
        {
            var mergable = _factory();
            mergable.Id = Id;
            mergable.Merge = Merge;
            return mergable;
        }
    }

    public static class TestCaseExtensions
    {
        public static IEnumerable<IDataSource> DataSources(this TestCase testCase, TestBundle testBundle)
        {
            return
                (from id in testCase.DataSourceIds
                 join ds in testBundle.DataSources on id equals ds.Id
                 select ds).Distinct();
        }

        public static IEnumerable<IMessage> Messages(this TestCase testCase, TestBundle testBundle)
        {
            return
                (from id in testCase.MessageIds
                 join alert in testBundle.Messages on id equals alert.Id
                 select alert).Distinct();
        }

        public static IEnumerable<IReport> Reports(this TestCase testCase, TestBundle testBundle)
        {
            return
                (from id in testCase.Messages(testBundle).SelectMany(alert => alert.ReportIds)
                 join report in testBundle.Reports on id equals report.Id
                 select report).Distinct();
        }

        public static bool CanExecute(this TestCase testCase, IEnumerable<SoftString> profiles)
        {
            
            // In order for a test to be runnable it has to be enabled and its profile needs to match the list or the list needs to be empty.

            return
                testCase.Enabled &&
                ProfileMatches();

            bool ProfileMatches()
            {
                return
                    profiles.Empty() ||
                    profiles.Any(runnableProfile => testCase.Profiles.Contains(runnableProfile));
            }
        }
    }
}

