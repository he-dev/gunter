using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Data;
using Gunter.Services.DataFilters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Reusable;
using Reusable.Extensions;
using Reusable.IOnymous;
using Reusable.OmniLog;
using Reusable.Utilities.JsonNet;
using Reusable.Utilities.JsonNet.Converters;

namespace Gunter.Services
{
    internal interface ITestFileSerializer
    {
        Task<TestBundle> DeserializeAsync(Stream testFileStream);
    }

    internal class TestFileSerializer : ITestFileSerializer
    {
        private static readonly IEnumerable<Type> BuiltInTypes = new[]
        {
            typeof(Gunter.Data.SqlClient.TableOrView),
            typeof(Gunter.Services.DataFilters.GetJsonValue),
            typeof(Gunter.Services.DataFilters.GetFirstLine),
            typeof(Gunter.Services.Messengers.Mailr),
            typeof(Gunter.Reporting.Modules.Level),
            typeof(Gunter.Reporting.Modules.Greeting),
            typeof(Gunter.Reporting.Modules.TestCase),
            typeof(Gunter.Reporting.Modules.DataSource),
            typeof(Gunter.Reporting.Modules.DataSummary),
            typeof(Gunter.Reporting.Formatters.TimeSpan),
        };
        
        private readonly JsonSerializer _jsonSerializer;

        private static readonly JsonVisitor Transform;

        static TestFileSerializer()
        {
            Transform = JsonVisitor.CreateComposite
            (
                new TrimPropertyNameVisitor(),
                new RewriteTypeVisitor(new PrettyTypeResolver(TypeDictionary.From(BuiltInTypes)))
            );
        }

        public TestFileSerializer(IContractResolver contractResolver)
        {
            _jsonSerializer = new JsonSerializer
            {
                ContractResolver = contractResolver,
                DefaultValueHandling = DefaultValueHandling.Populate,
                TypeNameHandling = TypeNameHandling.Auto,
                ObjectCreationHandling = ObjectCreationHandling.Reuse,
                Converters =
                {
                    //new MergeConverter(),
                    new LambdaJsonConverter<LogLevel>
                    {
                        ReadJsonCallback = LogLevel.FromName
                    },
                    new JsonStringConverter()
                }
            };
        }

        public async Task<TestBundle> DeserializeAsync(Stream testFileStream)
        {
            using (var streamReader = new StreamReader(testFileStream.Rewind()))
            {
                var json = await streamReader.ReadToEndAsync();
                return Transform.Visit(json).ToObject<TestBundle>(_jsonSerializer);
            }
        }
    }
}