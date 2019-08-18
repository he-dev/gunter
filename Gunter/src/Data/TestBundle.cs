using System;
using System.Collections.Generic;
using Gunter.Data;
using Newtonsoft.Json;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.IO;
using Gunter.Reporting;
using Gunter.Reporting.Modules.Tabular;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable;

namespace Gunter.Data
{
    [PublicAPI]
    [JsonObject]
    public class TestBundle : IEnumerable<IEnumerable<IMergeable>>
    {
        public const string PartialPrefix = "_";
        
        public static readonly IEnumerable<Type> KnownTypes = new[]
        {
            typeof(Gunter.Data.SqlClient.TableOrView),
            typeof(Gunter.Services.DataFilters.GetJsonValue),
            typeof(Gunter.Services.DataFilters.GetFirstLine),
            typeof(Gunter.Services.Messengers.Mailr),
            typeof(Gunter.Reporting.Modules.Level),
            typeof(Gunter.Reporting.Modules.Greeting),
            typeof(TestInfo),
            typeof(QueryInfo),
            typeof(DataInfo),
            typeof(Gunter.Reporting.Formatters.TimeSpan),
        };
        
        [DefaultValue(true)]
        public bool Enabled { get; set; }

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<StaticPropertyCollection> Variables { get; set; } = new List<StaticPropertyCollection>();

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<IQuery> Queries { get; set; } = new List<IQuery>();

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<TestCase> Tests { get; set; } = new List<TestCase>();

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<IChannel> Channels { get; set; } = new List<IChannel>();

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<IReport> Reports { get; set; } = new List<IReport>();

        [JsonIgnore]
        public string FullName { get; set; }

        [NotNull, JsonIgnore]
        public string DirectoryName => Path.GetDirectoryName(FullName);

        [NotNull, JsonIgnore]
        public SoftString FileName => Path.GetFileNameWithoutExtension(FullName);

        [JsonIgnore]
        public SoftString Name => Path.GetFileNameWithoutExtension(FileName.ToString());

        public TestBundleType Type =>
            FullName is null
                ? TestBundleType.Unknown
                : Path.GetFileName(FullName).StartsWith(PartialPrefix)
                    ? TestBundleType.Partial
                    : TestBundleType.Regular;

        public IEnumerator<IEnumerable<IMergeable>> GetEnumerator()
        {
            yield return Variables;
            yield return Queries;
            yield return Tests;
            yield return Channels;
            yield return Reports;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}