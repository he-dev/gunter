using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Gunter.Data.Configuration.Abstractions;
using Gunter.Data.Configuration.Reporting;
using Gunter.Services;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;

namespace Gunter.Data.Configuration
{
    [PublicAPI]
    [JsonObject]
    public class Theory : IModel, IMergeable, IEnumerable<IModel>
    {
        public const string TemplatePrefix = "_";

        public static readonly IEnumerable<Type> SectionTypes = new[]
        {
            typeof(TableOrView),
            typeof(Gunter.Services.DataFilters.GetJsonValue),
            typeof(Gunter.Services.DataFilters.GetFirstLine),
            typeof(DispatchEmail),
            typeof(Level),
            typeof(TestInfo),
            typeof(QueryInfo),
            typeof(DataSummary),
            typeof(Gunter.Reporting.Formatters.TimeSpan),
        };

        public SoftString Name { get; set; }

        public List<TemplateSelector>? TemplateSelectors { get; set; }

        [DefaultValue(true)]
        public bool Enabled { get; set; }

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IEnumerable<IPropertyCollection> Properties { get; set; }

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IEnumerable<IQuery> Queries { get; set; } = new List<IQuery>();

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IEnumerable<ITestCase> Tests { get; set; } = new List<ITestCase>();

        // [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        // public List<ISend> Channels { get; set; } = new List<ISend>();

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IEnumerable<IReport> Reports { get; set; } = new List<IReport>();

        [JsonIgnore]
        public string FullName { get; set; }

        [JsonIgnore]
        public string DirectoryName => Path.GetDirectoryName(FullName);

        [JsonIgnore]
        public string FileName => Path.GetFileNameWithoutExtension(FullName);

        //[JsonIgnore]
        //public string Name => Path.GetFileNameWithoutExtension(FileName);

        [JsonIgnore]
        public TheoryType Type =>
            Name is null
                ? TheoryType.Unknown
                : Path.GetFileName(Name.ToString()).StartsWith(TemplatePrefix)
                    ? TheoryType.Template
                    : TheoryType.Regular;
        
        public IEnumerator<IModel> GetEnumerator()
        {
            return Properties.Cast<IModel>().Concat(Queries).Concat(Tests).Concat(Reports).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}