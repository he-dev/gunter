using System.Collections.Generic;
using System.ComponentModel;
using Gunter.Annotations;
using Gunter.Data.Abstractions;
using Gunter.Data.Configuration.Abstractions;

namespace Gunter.Data.Configuration
{
    [Gunter]
    public class Email : IMessage, IMergeable
    {
        public string? Name { get; set; }

        public ModelSelector? ModelSelector { get; set; }

        public List<string> To { get; set; } = new List<string>();

        public List<string> CC { get; set; } = new List<string>();

        [DefaultValue("default")]
        public string Theme { get; set; }
        
        public string? ReportName { get; set; }
    }

    [Gunter]
    public class Halt : IMessage
    {
        public string? Name { get; set; }

        public string? ReportName { get; set; }
    }
}