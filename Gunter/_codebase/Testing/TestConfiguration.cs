using System.Collections.Generic;
using Gunter.Data;
using Gunter.Alerting;
using Newtonsoft.Json;

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
            Profiles = new Dictionary<string, string[]>();
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

        public Dictionary<string, string[]> Profiles { get; set; }
    }

    public class TestProperties
    {
        public string Name { get; set; }

        public bool Enabled { get; set; }

        public Severity Severity { get; set; }

        [JsonRequired]
        public string Message { get; set; }

        [JsonRequired]
        public int[] DataSources { get; set; }

        public string Filter { get; set; }

        public bool Assert { get; set; }

        [JsonRequired]
        public string Expression { get; set; }

        public bool CanContinue { get; set; }

        public List<int> Alerts { get; set; } = new List<int>();
    }

    public enum Severity
    {
        Critical,
        Warning
    }

    //public enum AssertLogic
    //{
    //    IsTrue,
    //    IsFalse
    //}

    //public enum OnFailure
    //{
    //    Break,
    //    Continue
    //}

    //public enum AlertWhen
    //{
    //    Failed,
    //    Passed
    //}
}
