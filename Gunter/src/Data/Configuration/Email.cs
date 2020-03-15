using System.Collections.Generic;
using System.ComponentModel;
using Gunter.Annotations;
using Gunter.Data.Configuration.Abstractions;
using Newtonsoft.Json;
using Reusable;

namespace Gunter.Data.Configuration
{
    [Gunter]
    public class Email : IMessage, IMergeable
    {
        public SoftString? Name { get; set; }

        public TemplateSelector? TemplateSelector { get; set; }

        public List<string> To { get; set; } = new List<string>();

        public List<string> CC { get; set; } = new List<string>();

        [DefaultValue("default")]
        public string Theme { get; set; }
        
        public string? ReportName { get; set; }
    }

    [Gunter]
    public class Halt : IMessage
    {
        public SoftString? Name { get; set; }

        public string? ReportName { get; set; }
    }
}