using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Gunter.Annotations;
using Gunter.Data.Abstractions;
using JetBrains.Annotations;
using Reusable;
using Reusable.Data;
using Reusable.Extensions;
using Reusable.OmniLog;

namespace Gunter.Data
{
    public interface ITestCase : IModel
    {
        LogLevel Level { get; }
        string Message { get; }
        List<SoftString> QueryIds { get; }
        string Filter { get; }
        string Assert { get; }
        Dictionary<TestResult, List<string>> When { get; }
        HashSet<SoftString> Tags { get; }
    }

    [Gunter]
    [PublicAPI]
    public class TestCase : ITestCase
    {
        private readonly Factory _factory;

        public delegate TestCase Factory();

        public TestCase(Factory factory) => _factory = factory;

        public Specification Parent { get; }

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
        public List<SoftString> QueryIds { get; set; } = new List<SoftString>();

        [Mergeable]
        public string Filter { get; set; }

        [Mergeable]
        public string Assert { get; set; }

        // Gets or sets commands that should be executed upon the specified test-result.
        public Dictionary<TestResult, List<string>> When { get; set; } = new Dictionary<TestResult, List<string>>();

        //[JsonProperty("Profiles", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [Mergeable]
        public HashSet<SoftString> Tags { get; set; } = new HashSet<SoftString>();
    }

    public class TestCaseUnion : Union<ITestCase>, ITestCase
    {
        public TestCaseUnion(ITestCase model, IEnumerable<Specification> templates) : base(model, templates) { }

        public LogLevel Level => GetValue(x => x.Level, x => x > LogLevel.None);
        public string Message => GetValue(x => x.Message, x => x is {});
        public List<SoftString> QueryIds => GetValue(x => x.QueryIds, x => x?.Any() == true);
        public string Filter => GetValue(x => x.Filter, x => x is {});
        public string Assert => GetValue(x => x.Assert, x => x is {});
        public Dictionary<TestResult, List<string>> When => GetValue(x => x.When, x => x?.Any() == true);
        public HashSet<SoftString> Tags => GetValue(x => x.Tags, x => x?.Any() == true);
    }

    public static class TestCaseExtensions
    {
        public static IEnumerable<IQuery> Queries(this TestCase testCase, Specification specification)
        {
            var dataSources =
                from id in testCase.QueryIds
                join ds in specification.Queries on id equals ds.Id
                select ds;

            return dataSources.Distinct();
        }

        public static bool IsNullOr<T>(this IEnumerable<T> source, Predicate<IEnumerable<T>> predicate)
        {
            return source is null || predicate(source);
        }
    }

    public class TestWhen
    {
        [JsonProperty("Send", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IEnumerable<SoftString>? MessengerIds { get; set; }
    }
}