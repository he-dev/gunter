using System.Collections;
using System.Collections.Generic;
using Gunter.Annotations;
using Gunter.Data.Configuration.Abstractions;
using Gunter.Data.Configuration.Reports.CustomSections.Abstractions;
using Newtonsoft.Json;

namespace Gunter.Data.Configuration.Reports
{
    [Gunter]
    [JsonObject]
    public class Custom : IReport, IEnumerable<CustomSection>
    {
        public string Name { get; set; }

        public ModelSelector? ModelSelector { get; set; }

        public string Title { get; set; }

        public List<CustomSection> Modules { get; set; } = new List<CustomSection>();

        public IEnumerator<CustomSection> GetEnumerator() => Modules.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}