using System.Collections.Generic;
using Newtonsoft.Json;
using System.ComponentModel;

namespace Gunter.Data
{
    public class TestProperties
    {
        [DefaultValue(true)]
        public bool Enabled { get; set; }

        [DefaultValue(TestSeverity.Warning)]
        public TestSeverity Severity { get; set; }

        [JsonRequired]
        public string Message { get; set; }

        [JsonRequired]
        public List<int> DataSources { get; set; } = new List<int>();

        public string Filter { get; set; }

        [DefaultValue(true)]
        public bool Assert { get; set; }

        [JsonRequired]
        public string Expression { get; set; }

        [DefaultValue(true)]
        public bool CanContinue { get; set; }

        public List<int> Alerts { get; set; } = new List<int>();

        public List<string> Profiles { get; set; } = new List<string>();
    }
}
