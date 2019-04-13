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
    [Gunter]
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

        // Gets or sets commands that should be executed upon the specified test-result.
        public IDictionary<TestResult, IList<string>> When { get; set; }

        //[JsonProperty("Profiles", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [Mergeable]
        public IList<SoftString> Tags { get; set; } = new List<SoftString>();
    }

    public static class TestCaseExtensions
    {
        public static IEnumerable<ILog> DataSources(this TestCase testCase, TestBundle testBundle)
        {
            return
            (
                from id in testCase.DataSourceIds
                join ds in testBundle.Logs on id equals ds.Id
                select ds
            ).Distinct();
        }

        public static bool IsNullOr<T>(this IEnumerable<T> source, Predicate<IEnumerable<T>> predicate)
        {
            return source is null || predicate(source);
        }
    }

    public class TestWhen
    {
        [JsonProperty("Send", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [CanBeNull]
        public IEnumerable<SoftString> MessengerIds { get; set; }
    }
}