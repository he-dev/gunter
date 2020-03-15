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

        public static readonly IEnumerable<Type> DataTypes = new[]
        {
            typeof(TableOrView),
            typeof(Gunter.Services.DataFilters.GetJsonValue),
            typeof(Gunter.Services.DataFilters.GetFirstLine),
            typeof(Email),
            typeof(Halt),
            typeof(Level),
            typeof(TestInfo),
            typeof(QueryInfo),
            typeof(DataSummary),
            typeof(FormatTimeSpan),
        };

        public SoftString Name { get; set; }

        public TemplateSelector TemplateSelectors { get; set; }

        [DefaultValue(true)]
        public bool Enabled { get; set; }

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IEnumerable<ConstantPropertyCollection> Properties { get; set; }

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IEnumerable<IQuery> Queries { get; set; } = new List<IQuery>();

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IEnumerable<TestCase> Tests { get; set; } = new List<TestCase>();

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