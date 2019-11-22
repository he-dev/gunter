using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gunter.Annotations;
using Newtonsoft.Json;
using Reusable;

namespace Gunter.Data
{
    [JsonObject]
    public class StaticPropertyCollection : IEnumerable<StaticProperty>, IMergeable
    {
        private Dictionary<SoftString, object> _variables = new Dictionary<SoftString, object>();

        public SoftString Id { get; set; }

        public Merge Merge { get; set; }

        [Mergeable]
        public IDictionary<SoftString, object> Items { get; set; }

        public IEnumerator<StaticProperty> GetEnumerator() => Items.Select(x => (StaticProperty)x).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}