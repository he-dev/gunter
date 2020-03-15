using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Gunter.Annotations;
using Gunter.Data.Configuration.Abstractions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;
using Reusable.OmniLog;

namespace Gunter.Data.Configuration
{
    [Gunter]
    [PublicAPI]
    public class TestCase : IModel, IMergeable
    {
        private readonly Factory _factory;

        public delegate TestCase Factory();

        public TestCase(Factory factory) => _factory = factory;

        public Theory Parent { get; }

        public SoftString Name { get; set; }

        public TemplateSelector TemplateSelectors { get; set; }

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
    }

    public static class TestCaseExtensions
    {
        public static IEnumerable<IQuery> Queries(this TestCase testCase, Theory theory)
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