using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Custom;
using Gunter.Annotations;
using Gunter.Reporting;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable;
using Reusable.Extensions;
using Reusable.OmniLog;

namespace Gunter.Data
{
    [PublicAPI]
    public class TestCase : IMergeable
    {
        private readonly Factory _factory;

        public delegate TestCase Factory();

        public TestCase(Factory factory)
        {
            Debug.Assert(factory.IsNotNull());
            _factory = factory;
        }

        public SoftString Id { get; set; }

        public Merge Merge { get; set; }

        [DefaultValue(true)]
        public bool Enabled { get; set; }

        [Mergeable]
        public LogLevel Level { get; set; } = LogLevel.Warning;

        [Mergeable]
        public string Message { get; set; }

        [JsonProperty("Check", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [Mergeable]
        public IList<SoftString> DataSourceIds { get; set; } = new List<SoftString>();

        [Mergeable]
        public string Filter { get; set; }

        [Mergeable]
        public string Assert { get; set; }

        //[Mergeable]
        //public IDictionary<TestResult, TestWhen> When { get; set; }
        
        public IDictionary<TestResult, IList<string>> When { get; set; }

        [JsonProperty("Profiles", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [Mergeable]
        public IList<SoftString> Tags { get; set; } = new List<SoftString>();
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

//        public static IEnumerable<IMessenger> Messengers([CanBeNull] this TestWhen testWhen, TestBundle testBundle)
//        {
//            return
//            (
//                from id in testWhen?.MessengerIds ?? Enumerable.Empty<SoftString>()
//                join messenger in testBundle.Messengers on id equals messenger.Id
//                select messenger
//            ).Distinct();
//        }

//        public static IEnumerable<IReport> Reports(this TestWhen testWhen, TestBundle testBundle)
//        {
//            return
//            (
//                from id in testWhen.Messengers(testBundle).SelectMany(alert => alert.ReportIds)
//                join report in testBundle.Reports on id equals report.Id
//                select report
//            ).Distinct();
//        }


        public static bool IsNullOr<T>(this IEnumerable<T> source, Predicate<IEnumerable<T>> predicate)
        {
            return source is null || predicate(source);
        }
    }

    public class TestWhen
    {        
        public bool Halt { get; set; }

        [JsonProperty("Send", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [CanBeNull]
        public IEnumerable<SoftString> MessengerIds { get; set; }
    }
}