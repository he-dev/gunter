using System.Collections.Generic;
using Gunter.Data;
using Newtonsoft.Json;
using System.Linq;
using System;
using System.Collections;
using System.Data;
using System.IO;
using Gunter.Annotations;
using Gunter.Messaging;
using Gunter.Reporting;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable;

namespace Gunter.Data
{
    [PublicAPI]
    public class TestBundle
    {
        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<VariableSet> Variables { get; set; } = new List<VariableSet>();

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<IDataSource> DataSources { get; set; } = new List<IDataSource>();

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<TestCase> Tests { get; set; } = new List<TestCase>();

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<IMessage> Messages { get; set; } = new List<IMessage>();

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<IReport> Reports { get; set; } = new List<IReport>();

        [JsonIgnore]
        public string FullName { get; set; }

        [NotNull]
        [JsonIgnore]
        public SoftString FileName => Path.GetFileNameWithoutExtension(FullName);

        [JsonIgnore]
        public SoftString Name => Path.GetFileNameWithoutExtension(FileName.ToString());
    }

    public static class TestBundleExtensions
    {
        public static IEnumerable<KeyValuePair<SoftString, object>> AllVariables(this TestBundle testBundle) => testBundle.Variables.SelectMany(x => x);   
    }

    [JsonObject]
    public interface IVariableSet : IEnumerable<KeyValuePair<SoftString, object>> { }

    public class VariableSet : IVariableSet, IMergable
    {
        private Dictionary<SoftString, object> _variables = new Dictionary<SoftString, object>();

        public int Id { get; set; }

        public string Prefix { get; set; }

        public Merge Merge { get; set; }

        [Mergable]
        public IDictionary<SoftString, object> Items { get; set; }
        //{
        //    get => _variables;
        //    set
        //    {
        //        _variableNameValidator.ValidateNamesNotReserved(this.Select(x => x.Key));
        //        _variables = value;
        //    }
        //}

        public IEnumerator<KeyValuePair<SoftString, object>> GetEnumerator()
        {
            var prefix = Prefix is null ? default : $"{Prefix}.";
            return Items.Select(x => new KeyValuePair<SoftString, object>($"{prefix}{x.ToString()}", x.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }
}

