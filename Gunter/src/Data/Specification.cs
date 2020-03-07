using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections;
using System.ComponentModel;
using System.IO;
using Gunter.Reporting;
using Gunter.Reporting.Modules.Tabular;
using Gunter.Services;
using Gunter.Services.Channels;
using Gunter.Workflows;
using JetBrains.Annotations;
using Reusable;

namespace Gunter.Data
{
    [PublicAPI]
    [JsonObject]
    public class Specification : IEnumerable<IEnumerable<IModel>>
    {
        public const string TemplatePrefix = "_";

        public static readonly IEnumerable<Type> SectionTypes = new[]
        {
            typeof(Gunter.Data.SqlClient.TableOrView),
            typeof(Gunter.Services.DataFilters.GetJsonValue),
            typeof(Gunter.Services.DataFilters.GetFirstLine),
            typeof(Gunter.Services.Channels.Mailr),
            typeof(Gunter.Reporting.Modules.Level),
            typeof(Gunter.Reporting.Modules.Greeting),
            typeof(Gunter.Reporting.Modules.Tabular.TestInfo),
            typeof(Gunter.Reporting.Modules.Tabular.QueryInfo),
            typeof(Gunter.Reporting.Modules.Tabular.DataInfo),
            typeof(Gunter.Reporting.Formatters.TimeSpan),
        };

        [DefaultValue(true)]
        public bool Enabled { get; set; }

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<PropertyCollection> Variables { get; set; } = new List<PropertyCollection>();

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

        [JsonIgnore]
        public string DirectoryName => Path.GetDirectoryName(FullName);

        [JsonIgnore]
        public SoftString FileName => Path.GetFileNameWithoutExtension(FullName);

        [JsonIgnore]
        public TestFile TestFile { get; set; }

        [JsonIgnore]
        public SoftString Name => Path.GetFileNameWithoutExtension(FileName.ToString());


        public IEnumerator<IEnumerable<IModel>> GetEnumerator()
        {
            yield return Variables;
            yield return Queries;
            yield return Tests;
            yield return Channels;
            yield return Reports;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public interface IChild<out TParent>
    {
        TParent Parent { get; }
    }
}