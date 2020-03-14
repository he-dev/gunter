using System.Collections.Generic;
using System.ComponentModel;
using Gunter.Data.Configuration.Abstractions;
using Newtonsoft.Json;
using Reusable;

namespace Gunter.Data.Configuration
{
    public class Email : IMessage, IMergeable
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