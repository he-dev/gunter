using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gunter.Data;
using Gunter.Json.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Reusable;
using Reusable.Extensions;
using Reusable.Utilities.JsonNet;

namespace Gunter.Services
{
    internal interface ITestFileSerializer
    {
        TestBundle Deserialize(Stream testFileStream);
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
                    typeof(Gunter.Data.Attachements.JsonValue),
                    typeof(Gunter.Messaging.Mailr),
                    typeof(Gunter.Reporting.Modules.Level),
                    typeof(Gunter.Reporting.Modules.Greeting),
                    typeof(Gunter.Reporting.Modules.TestCase),
                    typeof(Gunter.Reporting.Modules.DataSource),
                    typeof(Gunter.Reporting.Modules.DataSummary),
                    typeof(Gunter.Reporting.Formatters.TimeSpan),
                    typeof(Gunter.Reporting.Filters.FirstLine),
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

        public TestBundle Deserialize(Stream testFileStream)
        {
            using (var streamReader = new StreamReader(testFileStream))
            {
                var json = streamReader.ReadToEnd();
                return Transform.Visit(json).ToObject<TestBundle>(_jsonSerializer);
            }
        }
    }   
}