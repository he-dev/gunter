using System;
using Gunter.Data.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Reusable.Utilities.JsonNet;
using Reusable.Utilities.JsonNet.Converters;

namespace Gunter.Services
{
    internal class DeserializeTestFile
    {
        public delegate DeserializeTestFile Factory(string fileName);

        public DeserializeTestFile(IPrettyJson prettyJson, IContractResolver contractResolver, string fileName)
        {
            PrettyJson = prettyJson;
            JsonSerializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                ContractResolver = contractResolver,
                Converters =
                {
                    new StringEnumConverter(),
                    new SoftStringConverter(),
                    new TestFileConverter
                    {
                        FileName = fileName
                    }
                }
            };
        }

        private IPrettyJson PrettyJson { get; }

        private JsonSerializer JsonSerializer { get; }

        public Theory Invoke(string prettyJson)
        {
            var normalJson = PrettyJson.Read(prettyJson, TypeDictionary.From(Theory.SectionTypes));
            return normalJson.ToObject<Theory>(JsonSerializer);
        }

        private class TestFileConverter : CustomCreationConverter<Theory>
        {
            public string FileName { get; set; }

            public override Theory Create(Type objectType)
            {
                return new Theory
                {
                    FullName = FileName
                };
            }
        }
    }
}