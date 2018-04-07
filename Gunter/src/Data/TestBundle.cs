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
    public class TestBundle
    {
        private readonly IVariableNameValidator _variableNameValidator;

        private Dictionary<SoftString, object> _variables = new Dictionary<SoftString, object>();

        public delegate TestBundle Factory(TestBundle otherBundle);

        public TestBundle(IVariableNameValidator variableNameValidator)
        {
            _variableNameValidator = variableNameValidator;
        }

        //public TestBundle(IVariableNameValidator variableNameValidator, TestBundle otherBundle)
        //    : this(variableNameValidator)
        //{
        //    FullName = otherBundle.FullName;
        //    Variables = otherBundle.Variables;
        //}

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<SoftString, object> Variables
        {
            get => _variables;
            set
            {
                _variableNameValidator.ValidateNamesNotReserved(value.Keys);
                _variables = value;
            }
        }

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
        public SoftString FileName => Path.GetFileName(FullName);

        [JsonIgnore]
        public SoftString Name => Path.GetFileNameWithoutExtension(FileName.ToString());
    }
}

