using System.Collections.Generic;
using Gunter.Data;
using Newtonsoft.Json;
using System.Linq;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.IO;
using Gunter.Annotations;
using Gunter.Messaging;
using Gunter.Messaging.Abstractions;
using Gunter.Reporting;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable;

namespace Gunter.Data
{
    [PublicAPI]
    [JsonObject]
    public class TestBundle : IEnumerable<IEnumerable<IMergeable>>
    {
        [DefaultValue(true)]
        public bool Enabled { get; set; }
        
        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<VariableCollection> Variables { get; set; } = new List<VariableCollection>();

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

        public IEnumerator<IEnumerable<IMergeable>> GetEnumerator()
        {
            yield return Variables;
            yield return DataSources;
            yield return Tests;
            yield return Messages;
            yield return Reports;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [JsonObject]
    public class VariableCollection : IEnumerable<KeyValuePair<SoftString, object>>, IMergeable
    {
        private Dictionary<SoftString, object> _variables = new Dictionary<SoftString, object>();

        public SoftString Id { get; set; }

        //public string Prefix { get; set; }

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

        public IEnumerator<KeyValuePair<SoftString, object>> GetEnumerator() => Items.GetEnumerator();
        //{
        //var prefix = Prefix is null ? default : $"{Prefix}.";
        //return Items.Select(x => new KeyValuePair<SoftString, object>($"{prefix}{x.ToString()}", x.Value)).GetEnumerator();
        //return Items.GetEnumerator();
        //}

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}