using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.ComponentModel;
using Gunter.Services;
using JetBrains.Annotations;

namespace Gunter.Data
{
    [PublicAPI]
    public class TestCase : IResolvable
    {
        private string _message;

        [JsonIgnore]
        public IVariableResolver Variables { get; set; } = VariableResolver.Empty;

        [DefaultValue(true)]
        public bool Enabled { get; set; }

        [DefaultValue(TestSeverity.Warn)]
        public TestSeverity Severity { get; set; }

        [JsonRequired]
        public string Message
        {
            get => Variables.Resolve(_message);
            set => _message = value;
        }

        [JsonRequired]
        public List<int> DataSources { get; set; } = new List<int>();

        public string Filter { get; set; }

        [JsonRequired]
        public string Expression { get; set; }

        [DefaultValue(true)]
        public bool Assert { get; set; }

        [DefaultValue(TestResultActions.None)]
        public TestResultActions OnPassed { get; set; }

        [DefaultValue(TestResultActions.Alert | TestResultActions.Halt)]
        public TestResultActions OnFailed { get; set; }

        public List<int> Alerts { get; set; } = new List<int>();

        public List<string> Profiles { get; set; } = new List<string>();

        [JsonIgnore]
        public TimeSpan Elapsed { get; set; }
    }
}

