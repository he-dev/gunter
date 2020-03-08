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
    public interface ITestCase : IModel, IMergeable
    {
        LogLevel Level { get; }

        HashSet<SoftString> QueryNames { get; }

        string Message { get; }

        string Filter { get; }

        string Assert { get; }

        [JsonProperty("When")]
        Dictionary<TestResult, List<IMessage>> Messages { get; }

        HashSet<SoftString> Tags { get; }
    }

    [Gunter]
    [PublicAPI]
    public class TestCase : ITestCase
    {
        private readonly Factory _factory;

        public delegate TestCase Factory();

        public TestCase(Factory factory) => _factory = factory;

        public Theory Parent { get; }

        public SoftString Name { get; set; }

        public List<TemplateSelector>? TemplateSelectors { get; set; }

        [DefaultValue(true)]
        public bool Enabled { get; set; }

        public LogLevel Level { get; set; } = LogLevel.Warning;

        public string Message { get; set; }

        [JsonProperty("Check", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public HashSet<SoftString> QueryNames { get; set; } = new HashSet<SoftString>();

        public string Filter { get; set; }

        public string Assert { get; set; }

        // Gets or sets commands that should be executed upon the specified test-result.
        public Dictionary<TestResult, List<IMessage>> Messages { get; set; } = new Dictionary<TestResult, List<IMessage>>();

        //[JsonProperty("Profiles", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public HashSet<SoftString> Tags { get; set; } = new HashSet<SoftString>();

        public IModel Merge(IEnumerable<Theory> templates) => new Union(this, templates);

        private class Union : Union<ITestCase>, ITestCase
        {
            public Union(ITestCase model, IEnumerable<Theory> templates) : base(model, templates) { }

            public LogLevel Level => GetValue(x => x.Level, x => x > LogLevel.None);

            public HashSet<SoftString> QueryNames => GetValue(x => x.QueryNames, x => x?.Any() == true);

            public string Message => GetValue(x => x.Message, x => x is {});

            public string Filter => GetValue(x => x.Filter, x => x is {});

            public string Assert => GetValue(x => x.Assert, x => x is {});

            public Dictionary<TestResult, List<IMessage>> Messages => GetValue(x => x.Messages, x => x?.Any() == true);

            public HashSet<SoftString> Tags => GetValue(x => x.Tags, x => x?.Any() == true);

            public IModel Merge(IEnumerable<Theory> templates) => new Union(this, templates);
        }
    }

    public static class TestCaseExtensions
    {
        public static IEnumerable<IQuery> Queries(this ITestCase testCase, Theory theory)
        {
            var dataSources =
                from id in testCase.QueryNames
                join ds in theory.Queries on id equals ds.Name
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