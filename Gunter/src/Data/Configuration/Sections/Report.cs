using System.Collections;
using System.Collections.Generic;
using Gunter.Data.Configuration.Abstractions;
using Newtonsoft.Json;

namespace Gunter.Data.Configuration.Sections
{
    [JsonObject]
    public class Report : IReport, IEnumerable<ReportModule>
    {
        [JsonRequired]
        public string? Name { get; set; }

        public ModelSelector ModelSelector { get; set; }

        public string Title { get; set; }

        public List<ReportModule> Modules { get; set; } = new List<ReportModule>();

        public IEnumerator<ReportModule> GetEnumerator() => Modules.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}