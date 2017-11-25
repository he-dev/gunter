using System.Collections.Generic;
using Gunter.Data;
using Newtonsoft.Json;
using System.Linq;
using System;
using System.Data;
using System.IO;
using Gunter.Messaging;
using Gunter.Reporting;
using JetBrains.Annotations;
using Reusable;

namespace Gunter.Data
{
    [PublicAPI]
    public class TestFile
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<SoftString, object> Locals { get; set; } = new Dictionary<SoftString, object>();

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<IDataSource> DataSources { get; set; } = new List<IDataSource>();

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<TestCase> Tests { get; set; } = new List<TestCase>();

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<IMessage> Messages { get; set; } = new List<IMessage>();

        [JsonRequired, JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<IReport> Reports { get; set; } = new List<IReport>();

        [JsonIgnore]
        public string FullName { get; set; }

        [NotNull]
        [JsonIgnore]
        public string FileName => Path.GetFileName(FullName);
    }
}

