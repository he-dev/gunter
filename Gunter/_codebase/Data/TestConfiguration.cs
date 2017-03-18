using System.Collections.Generic;
using Gunter.Data;
using Gunter.Alerts;
using Newtonsoft.Json;
using System.Linq;
using System;
using System.ComponentModel;
using Gunter.Services;

namespace Gunter.Data
{
    public class TestConfiguration
    {
        public TestConfiguration()
        {
            Locals = new Dictionary<string, object>();
            DataSources = new List<IDataSource>();
            Tests = new List<TestProperties>();
            Alerts = new List<IAlert>();
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

        //public bool ContainsProfile(string name) => string.IsNullOrEmpty(name) || Profiles?.Any(x => x.Equals(name, StringComparison.OrdinalIgnoreCase)) == true;

        [JsonIgnore]
        public IEnumerable<TestProperties> EnabledTests => Tests.Where(x => x.Enabled);

        public IEnumerable<TestContext> ComposeTests(IConstantResolver constants)
        {
            var profileExists = constants.TryGetValue(Globals.Profile, out object profile);
            var results =
                from test in Tests
                where
                    test.Enabled &&
                    (!profileExists || test.Profiles.Contains((string)profile, StringComparer.OrdinalIgnoreCase))
                from id in test.DataSources
                join ds in DataSources on id equals ds.Id
                select new TestContext
                {
                    Test = test,
                    DataSource = ds,
                    Alerts =
                        from aId in test.Alerts
                        join a in Alerts on aId equals a.Id
                        select a,
                    Constants =
                        constants
                            .UnionWith(Locals)
                            .Add(Globals.Test.FileName, FileName)
                            .Add(Globals.Test.Severity, test.Severity)
                            .Add(Globals.Test.Message, test.Message)
                };
            return results;
        }
    }

    public class TestProperties
    {
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

    public class TestContext
    {
        public TestProperties Test { get; set; }

        public IDataSource DataSource { get; set; }

        public IEnumerable<IAlert> Alerts { get; set; }

        public IConstantResolver Constants { get; set; }
    }
}
