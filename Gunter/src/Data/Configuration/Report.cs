using System.Collections;
using System.Collections.Generic;
using Gunter.Annotations;
using Gunter.Data;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;

namespace Gunter.Reporting
{
    public interface IReport : IModel, IMergeable
    {
        string Title { get; }

        List<IReportModuleFactory> Modules { get; }
    }

    [JsonObject]
    public class Report : IReport, IEnumerable<IReportModuleFactory>
    {
        [JsonRequired]
        public SoftString Name { get; set; }

        public List<TemplateSelector>? TemplateSelectors { get; set; }

        public string Title { get; set; }

        public List<IReportModuleFactory> Modules { get; set; } = new List<IReportModuleFactory>();

        public IEnumerator<IReportModuleFactory> GetEnumerator() => Modules.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}