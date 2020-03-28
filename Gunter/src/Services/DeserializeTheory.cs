using System;
using Gunter.Data;
using Gunter.Data.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Reusable.Utilities.JsonNet;
using Reusable.Utilities.JsonNet.Converters;
using Reusable.Utilities.JsonNet.Visitors;

namespace Gunter.Services
{
    internal class DeserializeTheory
    {
        public DeserializeTheory(IContractResolver contractResolver)
        {
            CreateJsonSerializer = fileName => new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                DefaultValueHandling = DefaultValueHandling.Include,
                ContractResolver = contractResolver,
                Converters =
                {
                    new StringEnumConverter(),
                    new SoftStringConverter(),
                    new LambdaJsonConverter<ModelSelector>
                    {
                        ReadJsonCallback = ModelSelector.Parse
                    },
                    new TestFileConverter
                    {
                        FileName = fileName
                    }
                }
            };
        }

        private Func<string, JsonSerializer> CreateJsonSerializer { get; }

        public Theory Invoke(string fileName, string prettyJson)
        {
            var jsonVisitor = new CompositeJsonVisitor
            {
                new TrimPropertyName(),
                new NormalizePrettyTypeProperty(PrettyTypeDictionary.From(Theory.DataTypes))
            };
            return jsonVisitor.Visit(prettyJson).ToObject<Theory>(CreateJsonSerializer(fileName));
        }

        private class TestFileConverter : CustomCreationConverter<Theory>
        {
            public string FileName { get; set; }

            public override Theory Create(Type objectType)
            {
                return new Theory
                {
                    FileName = FileName
                };
            }
        }
    }
}