using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gunter.Data;
using Gunter.Data.Dtos;
using Gunter.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gunter.Json.Converters
{
    internal class MergeConverter : JsonConverter<Merge>
    {
        public override Merge ReadJson(JsonReader reader, Type objectType, Merge existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jToken = JToken.Load(reader);
            var mergeString = jToken.Value<string>();
            return mergeString is null ? default : Merge.Parse(mergeString);
        }

        public override void WriteJson(JsonWriter writer, Merge value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
    
    internal class TripleTableDtoConverter : JsonConverter<TripleTableDto>
    {
        public override TripleTableDto ReadJson(JsonReader reader, Type objectType, TripleTableDto existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
        
        public override void WriteJson(JsonWriter writer, TripleTableDto value, JsonSerializer serializer)
        {
            writer.WriteValue(value?.Dump());
        }
    }
}
