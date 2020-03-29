using System;
using System.Collections.Generic;
using System.Reflection;
using Gunter.Data.Configuration.Reports.CustomSections.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reusable.Extensions;

namespace Gunter.Data.ReportSections
{
    [UsePrettyType("Mailr")]
    public abstract class ReportSectionDto
    {
        protected ReportSectionDto(CustomSection section)
        {
            Tags = section.Tags;
        }

        public HashSet<string> Tags { get; }
    }

    public class UsePrettyTypeAttribute : Attribute
    {
        public UsePrettyTypeAttribute(string schema)
        {
            Schema = schema;
        }

        public string Schema { get; }
    }

    public class PrettyJsonConverter : JsonConverter
    {
        // Prevents circular serialization.
        private bool IsWriting { get; set; }

        public override bool CanConvert(Type objectType)
        {
            return !IsWriting && objectType.IsDefined(typeof(UsePrettyTypeAttribute), inherit: true);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            IsWriting = true;
            try
            {
                var schema = value.GetType().GetCustomAttribute<UsePrettyTypeAttribute>().Schema;
                var token = JObject.FromObject(value, serializer);
                token.AddFirst(new JProperty($"${schema}", value.GetType().ToPrettyString()));
                token.WriteTo(writer);
            }
            finally
            {
                IsWriting = false;
            }
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}