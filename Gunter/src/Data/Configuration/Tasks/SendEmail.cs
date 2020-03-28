using System.Collections.Generic;
using System.ComponentModel;
using Gunter.Annotations;
using Gunter.Data.Abstractions;
using Gunter.Data.Configuration.Abstractions;
using Newtonsoft.Json;

namespace Gunter.Data.Configuration.Tasks
{
    [Gunter]
    public class SendEmail : ITask, IMergeable
    {
        [JsonRequired]
        public string Name { get; set; } = default!;
        
        public ModelSelector? ModelSelector { get; set; }

        public List<string> To { get; set; } = new List<string>();

        public List<string> CC { get; set; } = new List<string>();
        
        public string Subject { get; set; } = $"{{Report.Title}}";

        public string Theme { get; set; } = "default";

        [JsonProperty("Report")]
        public string ReportName { get; set; } = default!;

    }
}