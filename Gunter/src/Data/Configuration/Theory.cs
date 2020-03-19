using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Gunter.Data.Abstractions;
using Gunter.Data.Configuration.Abstractions;
using Gunter.Data.Configuration.Reporting;
using Gunter.Data.Configuration.Sections;
using Gunter.Services;
using Gunter.Services.FilterData;
using JetBrains.Annotations;
using Newtonsoft.Json;

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
            typeof(GetJsonValue),
            typeof(GetFirstLine),
            typeof(Email),
            typeof(Halt),
            typeof(Level),
            typeof(Heading),
            typeof(Paragraph),
            typeof(TestSummary),
            typeof(QuerySummary),
            typeof(DataSummary),
            typeof(FormatTimeSpan),
        };

        [JsonIgnore]
        public string FileName { get; set; }

        [JsonIgnore]
        public string DirectoryName => Path.GetDirectoryName(FileName);

        [JsonIgnore]
        public string? Name
        {
            get => Path.GetFileNameWithoutExtension(FileName);
            set { }
        }

        public ModelSelector ModelSelector { get; set; }

        [DefaultValue(true)]
        public bool Enabled { get; set; }

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IEnumerable<PropertyCollection> Properties { get; set; }

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IEnumerable<IQuery> Queries { get; set; } = new List<IQuery>();

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IEnumerable<TestCase> Tests { get; set; } = new List<TestCase>();

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IEnumerable<IReport> Reports { get; set; } = new List<IReport>();

        [JsonIgnore]
        public TheoryType Type =>
            Name is null
                ? TheoryType.Unknown
                : Path.GetFileName(Name).StartsWith(TemplatePrefix)
                    ? TheoryType.Template
                    : TheoryType.Regular;

        public IEnumerator<IModel> GetEnumerator()
        {
            return Properties.Cast<IModel>().Concat(Queries).Concat(Tests).Concat(Reports).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}