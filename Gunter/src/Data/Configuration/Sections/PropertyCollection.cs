using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gunter.Data.Abstractions;
using Gunter.Data.Properties;
using Newtonsoft.Json;
using Reusable;

namespace Gunter.Data.Configuration.Sections
{
    [JsonObject]
    public class PropertyCollection : IEnumerable<IProperty>, IModel, IMergeable
    {
        public string Name { get; set; } = default!;

        public ModelSelector? ModelSelector { get; set; }

        public Dictionary<string, object> Items { get; set; } = new Dictionary<string, object>(SoftString.Comparer);

        public IEnumerator<IProperty> GetEnumerator() => Items.Select(x => new ConstantProperty(x.Key, x.Value)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}