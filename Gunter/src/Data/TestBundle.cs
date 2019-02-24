using System;
using System.Collections.Generic;
using Gunter.Data;
using Newtonsoft.Json;
using System.Linq;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.IO;
using Gunter.Annotations;
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
        public List<TestBundleVariableCollection> Variables { get; set; } = new List<TestBundleVariableCollection>();

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<ILog> Logs { get; set; } = new List<ILog>();

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<TestCase> Tests { get; set; } = new List<TestCase>();

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<IMessenger> Messengers { get; set; } = new List<IMessenger>();

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
                : Path.GetFileName(FullName).StartsWith("_")
                    ? TestBundleType.Partial
                    : TestBundleType.Regular;

        public IEnumerator<IEnumerable<IMergeable>> GetEnumerator()
        {
            yield return Variables;
            yield return Logs;
            yield return Tests;
            yield return Messengers;
            yield return Reports;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public interface IAssignment<out TName, out TValue>
    {
        TName Name { get; }

        TValue Value { get; }
    }

    [UsedImplicitly]
    public readonly struct TestBundleVariable : IAssignment<SoftString, object>
    {
        public TestBundleVariable(SoftString name, object value)
        {
            Name = name;
            Value = value;
        }

        public SoftString Name { get; }

        public object Value { get; }

        public static implicit operator TestBundleVariable(KeyValuePair<SoftString, object> kvp) => new TestBundleVariable(kvp.Key, kvp.Value);

        public static implicit operator KeyValuePair<SoftString, object>(TestBundleVariable tbv) => new KeyValuePair<SoftString, object>(tbv.Name, tbv.Value);
    }

    [JsonObject]
    public class TestBundleVariableCollection : IEnumerable<TestBundleVariable>, IMergeable
    {
        private Dictionary<SoftString, object> _variables = new Dictionary<SoftString, object>();

        public SoftString Id { get; set; }

        public Merge Merge { get; set; }

        [Mergeable]
        public IDictionary<SoftString, object> Dictionary { get; set; }

        public IEnumerator<TestBundleVariable> GetEnumerator() => Dictionary.Select(x => (TestBundleVariable)x).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}