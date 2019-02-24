using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Data;
using Gunter.Json.Converters;
using Gunter.Services.Messengers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Reusable;
using Reusable.Extensions;
using Reusable.IOnymous;
using Reusable.Utilities.JsonNet;

namespace Gunter.Services
{
    internal interface ITestFileSerializer
    {
        Task<TestBundle> DeserializeAsync(Stream testFileStream);
    }

    internal class TestFileSerializer : ITestFileSerializer
    {
        private readonly JsonSerializer _jsonSerializer;

        private static readonly JsonVisitor Transform;

        static TestFileSerializer()
        {
            Transform = JsonVisitor.CreateComposite
            (
                new PropertyNameTrimmer(),
                new PrettyTypeResolver(new[]
                {
                    typeof(Gunter.Data.SqlClient.TableOrView),
                    typeof(Gunter.Services.DataPostProcessors.ExtractJsonValue),
                    typeof(Gunter.Services.DataPostProcessors.FirstLine),
                    typeof(Gunter.Services.Messengers.Mailr),
                    typeof(Gunter.Reporting.Modules.Level),
                    typeof(Gunter.Reporting.Modules.Greeting),
                    typeof(Gunter.Reporting.Modules.TestCase),
                    typeof(Gunter.Reporting.Modules.DataSource),
                    typeof(Gunter.Reporting.Modules.DataSummary),
                    typeof(Gunter.Reporting.Formatters.TimeSpan),
                })
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
                    new MergeConverter()
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