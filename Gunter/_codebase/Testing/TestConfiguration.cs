using System.Collections.Generic;
using Gunter.Data;
using Gunter.Alerts;
using Newtonsoft.Json;
using System.Linq;
using System;
using System.ComponentModel;

namespace Gunter.Testing
{
    public class TestConfiguration
    {
        public TestConfiguration()
        {
            Locals = new Dictionary<string, object>();
            DataSources = new List<IDataSource>();
            Tests = new List<TestProperties>();
            Alerts = new List<IAlert>();
            Profiles = new List<string>();
        }

        [JsonIgnore]
        public string FileName { get; set; }

        public Dictionary<string, object> Locals { get; set; }

        [JsonRequired]
        public List<IDataSource> DataSources { get; set; } = new List<IDataSource>();

        [JsonRequired]
        public List<TestProperties> Tests { get; set; } = new List<TestProperties>();

        [JsonRequired]
        public List<IAlert> Alerts { get; set; }

        public List<string> Profiles { get; set; }

        public bool ContainsProfile(string name) => string.IsNullOrEmpty(name) || Profiles?.Any(x => x.Equals(name, StringComparison.OrdinalIgnoreCase)) == true;

        public IEnumerable<TestProperties> GetEnabledTests() => Tests.Where(x => x.Enabled);

        public IEnumerable<IDataSource> GetDataSources(IEnumerable<int> ids) => DataSources.Where(x => (ids ?? throw new ArgumentNullException(nameof(ids))).Contains(x.Id));

        public IEnumerable<IAlert> GetAlerts(IEnumerable<int> ids) => Alerts.Where(x => (ids ?? throw new ArgumentNullException(nameof(ids))).Contains(x.Id));
    }

    public class TestProperties
    {
        public string Name { get; set; }

        [DefaultValue(true)]
        public bool Enabled { get; set; }

        [DefaultValue(Severity.Warning)]
        public Severity Severity { get; set; }

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

    public enum Severity
    {
        Critical,
        Warning
    }    
}
