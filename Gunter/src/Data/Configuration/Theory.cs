using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Gunter.Data.Abstractions;
using Gunter.Data.Configuration.Abstractions;
using Gunter.Data.Configuration.Queries;
using Gunter.Data.Configuration.Reports;
using Gunter.Data.Configuration.Reports.CustomSections;
using Gunter.Data.Configuration.Sections;
using Gunter.Data.Configuration.Tasks;
using Gunter.Services;
using Gunter.Services.DataFilters;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Gunter.Data.Configuration
{
    [PublicAPI]
    [JsonObject]
    public class Theory : IModel, IEnumerable<IModel>
    {
        public const string TemplatePrefix = "_";

        public static readonly IEnumerable<Type> DataTypes = new[]
        {
            typeof(TableOrView),
            typeof(GetJsonValue),
            typeof(GetFirstLine),
            typeof(SendEmail),
            typeof(Halt),
            typeof(Custom),
            typeof(Heading),
            typeof(Paragraph),
            typeof(Level),
            typeof(Signature),
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
        public string Name
        {
            get => Path.GetFileNameWithoutExtension(FileName);
            set { }
        }

        [DefaultValue(true)]
        public bool Enabled { get; set; }

        [JsonRequired]
        public PropertyCollection Properties { get; set; }

        [JsonRequired]
        public IEnumerable<IQuery> Queries { get; set; } = new List<IQuery>();

        [JsonRequired]
        public IEnumerable<TestCase> Tests { get; set; } = new List<TestCase>();

        [JsonRequired]
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
            return new[] { Properties }.Cast<IModel>().Concat(Queries).Concat(Tests).Concat(Reports).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}