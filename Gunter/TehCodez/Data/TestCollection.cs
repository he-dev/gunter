using System.Collections.Generic;
using Gunter.Data;
using Newtonsoft.Json;
using System.Linq;
using System;
using Gunter.Services;
using System.Data;
using Gunter.Messaging;
using Gunter.Reporting;

namespace Gunter.Data
{
    public class TestCollection
    {
        public TestCollection()
        {
            Locals = new Dictionary<string, object>();
            DataSources = new List<IDataSource>();
            Tests = new List<TestCase>();
            Alerts = new List<IAlert>();
        }

        [JsonIgnore]
        public string FileName { get; set; }

        public Dictionary<string, object> Locals { get; set; }

        [JsonRequired]
        public List<IDataSource> DataSources { get; set; } = new List<IDataSource>();

        [JsonRequired]
        public List<TestCase> Tests { get; set; } = new List<TestCase>();

        [JsonRequired]
        public List<IAlert> Alerts { get; set; }

        [JsonRequired]
        public List<IReport> Reports { get; set; }
    }
}

