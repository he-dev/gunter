using System.Collections.Generic;
using Gunter.Data;
using Newtonsoft.Json;
using System.Linq;
using System;
using Gunter.Services;
using System.Data;
using Gunter.Messaging;
using Gunter.Reporting;
using JetBrains.Annotations;

namespace Gunter.Data
{
    [PublicAPI]
    public class TestFile
    {
        [JsonIgnore]
        public string FileName { get; set; }

        public Dictionary<string, object> Locals { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        [JsonRequired]
        public List<IDataSource> DataSources { get; set; } = new List<IDataSource>();

        [JsonRequired]
        public List<TestCase> Tests { get; set; } = new List<TestCase>();

        [JsonRequired]
        public List<IAlert> Alerts { get; set; } = new List<IAlert>();

        [JsonRequired]
        public List<IReport> Reports { get; set; } = new List<IReport>();
    }
}

