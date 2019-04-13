using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gunter.Annotations;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;

namespace Gunter.Data
{
    [PublicAPI]
    [UsedImplicitly]
    public readonly struct TestBundleVariable
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
        public IDictionary<SoftString, object> Items { get; set; }

        public IEnumerator<TestBundleVariable> GetEnumerator() => Items.Select(x => (TestBundleVariable)x).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}