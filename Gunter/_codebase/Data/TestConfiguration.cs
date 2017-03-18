using System.Collections.Generic;
using Gunter.Data;
using Gunter.Alerts;
using Newtonsoft.Json;
using System.Linq;
using System;
using Gunter.Services;
using System.Data;

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
    }

    
}

