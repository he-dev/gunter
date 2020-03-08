using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Gunter.Data.Abstractions;
using Gunter.Reporting;
using Gunter.Reporting.Modules.Tabular;
using Gunter.Services;
using Gunter.Services.Channels;
using Gunter.Workflows;
using JetBrains.Annotations;
using Reusable;

namespace Gunter.Data
{
    public interface ITheory : IModel, IMergeable, IEnumerable<IModel>
    {
        [DefaultValue(true)]
        bool Enabled { get; }

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        IEnumerable<IPropertyCollection> Properties { get; }

        IEnumerable<IQuery> Queries { get; }

        IEnumerable<ITestCase> Tests { get; }
    }

    [PublicAPI]
    [JsonObject]
    public class TheoryFile : ITheory
    {
        public const string TemplatePrefix = "_";

        public static readonly IEnumerable<Type> SectionTypes = new[]
        {
            typeof(Gunter.Data.SqlClient.TableOrView),
            typeof(Gunter.Services.DataFilters.GetJsonValue),
            typeof(Gunter.Services.DataFilters.GetFirstLine),
            typeof(Gunter.Services.Channels.SendEmail),
            typeof(Gunter.Reporting.Modules.Level),
            typeof(Gunter.Reporting.Modules.Greeting),
            typeof(Gunter.Reporting.Modules.Tabular.TestInfo),
            typeof(Gunter.Reporting.Modules.Tabular.QueryInfo),
            typeof(Gunter.Reporting.Modules.Tabular.DataInfo),
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
        public List<IReport> Reports { get; set; } = new List<IReport>();

        [JsonIgnore]
        public string FullName { get; set; }

        [JsonIgnore]
        public string DirectoryName => Path.GetDirectoryName(FullName);

        [JsonIgnore]
        public string FileName => Path.GetFileNameWithoutExtension(FullName);

        //[JsonIgnore]
        //public string Name => Path.GetFileNameWithoutExtension(FileName);

        [JsonIgnore]
        public TestFileType Type =>
            Name is null
                ? TestFileType.Unknown
                : Path.GetFileName(Name.ToString()).StartsWith(TemplatePrefix)
                    ? TestFileType.Template
                    : TestFileType.Regular;

        public IModel Merge(IEnumerable<TheoryFile> templates) => new Union(this, templates);

        public IEnumerator<IModel> GetEnumerator()
        {
            return Properties.Cast<IModel>().Concat(Queries).Concat(Tests).Concat(Reports).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private class Union : Union<ITheory>, ITheory
        {
            public Union(ITheory model, IEnumerable<TheoryFile> templates) : base(model, templates) { }

            public bool Enabled => Model.Enabled;

            public IEnumerable<IPropertyCollection> Properties => Model.Tests.Select(m => m.Merge(Templates)).Cast<IPropertyCollection>();

            public IEnumerable<IQuery> Queries => Model.Queries.Select(m => m is IMergeable mergeable ? mergeable.Merge(Templates) : m).Cast<IQuery>();

            public IEnumerable<ITestCase> Tests => Model.Tests.Select(m => m.Merge(Templates)).Cast<ITestCase>();

            public IModel Merge(IEnumerable<TheoryFile> templates) => new Union(this, templates);

            public IEnumerator<IModel> GetEnumerator() => Model.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}