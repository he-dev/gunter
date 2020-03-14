using System.Collections;
using System.Collections.Generic;
using Gunter.Data.Configuration.Abstractions;
using Gunter.Reporting;
using Newtonsoft.Json;
using Reusable;

namespace Gunter.Data.Configuration
{
    [JsonObject]
    public class Report : IReport, IEnumerable<ReportModule>
    {
        [JsonRequired]
        public SoftString Name { get; set; }

        public List<TemplateSelector>? TemplateSelectors { get; set; }

        public string Title { get; set; }

        public List<ReportModule> Modules { get; set; } = new List<ReportModule>();

        public IEnumerator<ReportModule> GetEnumerator() => Modules.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}