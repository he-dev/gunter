using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Gunter.Reporting;
using Newtonsoft.Json;
using Reusable;

namespace Gunter.Data
{
    public interface IMessage : IModel { }

    public interface IEmail : IMessage, IMergeable
    {
        List<string> To { get; }

        List<string> CC { get; }

        string Theme { get; }

        string ReportName { get; }
    }

    public class Email : IEmail
    {
        [JsonRequired]
        public SoftString Name { get; set; }

        public List<TemplateSelector>? TemplateSelectors { get; set; }

        public List<string> To { get; set; }

        public List<string> CC { get; set; }

        [DefaultValue("default")]
        public string Theme { get; set; }

        [JsonProperty("Report")]
        public string ReportName { get; set; }
    }
}