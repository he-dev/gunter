using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gunter.Annotations;
using Gunter.Data.Abstractions;
using Newtonsoft.Json;
using Reusable;

namespace Gunter.Data
{
    public interface IPropertyCollection : IEnumerable<StaticProperty>, IModel, IMergeable { }

    [JsonObject]
    public class StaticPropertyCollection : IPropertyCollection
    {
        public SoftString Name { get; set; }

        public List<TemplateSelector>? TemplateSelectors { get; set; }

        public IDictionary<string, object> Items { get; set; }

        public IEnumerator<StaticProperty> GetEnumerator() => Items.Select(x => (StaticProperty)x).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IModel Merge(IEnumerable<TheoryFile> templates) => new Union(this, templates);

        private class Union : Union<IPropertyCollection>, IPropertyCollection
        {
            public Union(IPropertyCollection model, IEnumerable<TheoryFile> templates) : base(model, templates) { }

            public IModel Merge(IEnumerable<TheoryFile> templates) => new Union(this, templates);

            public IEnumerator<StaticProperty> GetEnumerator() => base.Model.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}