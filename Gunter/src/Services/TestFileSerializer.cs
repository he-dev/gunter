using System.IO;
using Gunter.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Gunter
{
    internal interface ITestFileSerializer
    {
        TestBundle Deserialize(Stream testFileStream);
    }

    internal class TestFileSerializer : ITestFileSerializer
    {
        private readonly Newtonsoft.Json.JsonSerializer _jsonSerializer;

        public TestFileSerializer(IContractResolver contractResolver)
        {
            _jsonSerializer = new Newtonsoft.Json.JsonSerializer
            {
                ContractResolver = contractResolver,
                DefaultValueHandling = DefaultValueHandling.Populate,
                TypeNameHandling = TypeNameHandling.Auto,
                ObjectCreationHandling = ObjectCreationHandling.Reuse,
            };
        }

        public TestBundle Deserialize(Stream testFileStream)
        {
            using (var streamReader = new StreamReader(testFileStream))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                return _jsonSerializer.Deserialize<TestBundle>(jsonReader);
            }
        }
    }
}