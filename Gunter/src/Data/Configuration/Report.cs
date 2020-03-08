using System.Collections;
using System.Collections.Generic;
using Gunter.Annotations;
using Gunter.Data;
using Gunter.Data.Abstractions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;

namespace Gunter.Reporting
{
    public interface IReport : IModel, IMergeable<IReport>
    {
        string Title { get; }

        List<IModule> Modules { get; }
    }

    [JsonObject]
    public class Report : IReport, IEnumerable<IModule>
    {
        [JsonRequired]
        public SoftString Name { get; set; }

        public List<TemplateSelector>? TemplateSelectors { get; set; }

        public string Title { get; set; }

        public List<IModule> Modules { get; set; } = new List<IModule>();

        public IEnumerator<IModule> GetEnumerator() => Modules.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IModel Merge(IEnumerable<TheoryFile> templates) => new Union(this, templates);

        private class Union : Union<IReport>, IReport
        {
            public Union(IReport model, IEnumerable<TheoryFile> templates) : base(model, templates) { }

            public string Title => GetValue(x => x.Title, x => x is {});

            public List<IModule> Modules => GetValue(x => x.Modules, x => x is {});

            public IModel Merge(IEnumerable<TheoryFile> templates) => new Union(this, templates);
        }
    }
}