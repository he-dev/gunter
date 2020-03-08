using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Gunter.Data.Abstractions;
using Gunter.Reporting;
using Newtonsoft.Json;
using Reusable;

namespace Gunter.Data
{
    public interface IMessage : IModel { }

    public interface IEmail : IMessage, IMergeable<IEmail>
    {
        List<string> To { get; }

        List<string> CC { get; }

        string Theme { get; }
    }

    public class Email : IEmail
    {
        [JsonRequired]
        public SoftString Name { get; set; }

        public List<TemplateSelector>? TemplateSelectors { get; set; }

        public List<string> To { get; set; }

        public List<string> CC { get; set; }

        [JsonProperty("Reports")]
        public List<string> ReportNames { get; set; } = new List<string>();

        [DefaultValue("default")]
        public string Theme { get; set; }

        public IModel Merge(IEnumerable<TheoryFile> templates) => new Union(this, templates);

        private class Union : Union<IEmail>, IEmail
        {
            public Union(IEmail model, IEnumerable<TheoryFile> templates) : base(model, templates) { }

            public List<string> To => GetValue(x => x.To, x => x?.Any() == true);

            public List<string> CC => GetValue(x => x.CC, x => x?.Any() == true);

            public string Theme => GetValue(x => x.Theme, x => x is {});
            
            public IModel Merge(IEnumerable<TheoryFile> templates) => new Union(this, templates);
        }
    }
}