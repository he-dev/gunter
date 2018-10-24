using System;
using System.Data;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Reusable.Extensions;
using Reusable.Reflection;

namespace Gunter.Data.Attachements
{
    public interface IAttachment
    {
        string Name { get; set; }

        object Compute(DataRow source);
    }

    [UsedImplicitly]
    public class JsonValue : IAttachment
    {
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string JsonColumn { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string JsonPath { get; set; }

        public object Compute(DataRow source)
        {
            var json = source.Field<string>(JsonColumn);

            if (json.IsNullOrEmpty())
            {
                return default;
            }

            var jToken = JToken.Parse(json).SelectToken(JsonPath);
            switch (jToken)
            {
                case null: return default;
                case JProperty jProperty: return jProperty.Value;
                default: throw DynamicException.Create($"{Name}JsonPath", $"Expected {nameof(JProperty)} but found {jToken.GetType().Name}. {nameof(JsonPath)} must select a single value from {JsonColumn}. ");
            }
        }
    }
}