using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gunter.Data.Abstractions;
using Gunter.Data.Properties;
using Newtonsoft.Json;

namespace Gunter.Data.Configuration.Sections
{
    [JsonObject]
    public class PropertyCollection : IEnumerable<IProperty>, IModel, IMergeable
    {
        public string? Name { get; set; }

        public ModelSelector ModelSelector { get; set; }

        public IDictionary<string, object> Items { get; set; }

        public IEnumerator<IProperty> GetEnumerator() => Items.Select(x => new ConstantProperty(x.Key, x.Value)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}