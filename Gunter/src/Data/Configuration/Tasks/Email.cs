using System.Collections.Generic;
using System.ComponentModel;
using Gunter.Annotations;
using Gunter.Data.Abstractions;
using Gunter.Data.Configuration.Abstractions;
using Newtonsoft.Json;

namespace Gunter.Data.Configuration.Tasks
{
    [Gunter]
    public class Email : ITask, IMergeable
    {
        public string Name { get; set; }

        public ModelSelector? ModelSelector { get; set; }

        public List<string> To { get; set; } = new List<string>();

        public List<string> CC { get; set; } = new List<string>();

        [DefaultValue("default")]
        public string Theme { get; set; } = default!;

        [JsonProperty("Report")]
        public string ReportName { get; set; } = default!;
    }
}