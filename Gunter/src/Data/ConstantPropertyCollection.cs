using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gunter.Annotations;
using Newtonsoft.Json;
using Reusable;

namespace Gunter.Data
{
    public interface IPropertyCollection : IEnumerable<ConstantProperty>, IModel, IMergeable { }

    [JsonObject]
    public class ConstantPropertyCollection : IPropertyCollection
    {
        public SoftString Name { get; set; }

        public List<TemplateSelector>? TemplateSelectors { get; set; }

        public IDictionary<string, object> Items { get; set; }

        public IEnumerator<ConstantProperty> GetEnumerator() => Items.Select(x => new ConstantProperty(x.Key, x.Value)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}