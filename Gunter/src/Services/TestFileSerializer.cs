using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gunter.Data;
using Gunter.Json.Converters;
using Gunter.Messaging;
using Newtonsoft.Json;
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
        private readonly Newtonsoft.Json.JsonSerializer _jsonSerializer;

        public TestFileSerializer(IContractResolver contractResolver)
        {
            _jsonSerializer = new Newtonsoft.Json.JsonSerializer
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
            using (var jsonReader = new PrettyTypeReader(streamReader, "$t", PrettyTypeResolver.Create()))
            {
                return _jsonSerializer.Deserialize<TestBundle>(jsonReader);
            }
        }
    }

    public static class PrettyTypeResolver
    {
        private static readonly IEnumerable<Type> Types = new[]
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
        };

        public static Func<string, Type> Create()
        {
            var types = (from type in Types let prettyName = type.ToPrettyString() select (type, prettyName)).ToList();
            return prettyName => types.SingleOrDefault(t => SoftString.Comparer.Equals(t.prettyName, prettyName)).type;
        }
    }
}