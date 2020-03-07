using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gunter.Annotations;
using Newtonsoft.Json;
using Reusable;

namespace Gunter.Data
{
    [JsonObject]
    public class PropertyCollection : IEnumerable<IProperty>, IModel
    {
        public SoftString Id { get; set; }

        public Merge Merge { get; set; }

        [Mergeable]
        public IDictionary<string, object> Items { get; set; }

        public IEnumerator<IProperty> GetEnumerator() => Items.Select(x => (StaticProperty)x).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}