using System.ComponentModel;
using System.Data;
using Gunter.Data.Attachements.Abstractions;
using Gunter.Extensions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reusable.Exceptionizer;
using Reusable.Reflection;

namespace Gunter.Data.Attachements
{
    [UsedImplicitly]
    public class JsonValue : IAttachment
    {
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Column { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Path { get; set; }

        [DefaultValue(true)]
        public bool Strict { get; set; }

        public object Compute(DataRow source)
        {
            var value = source.Field<string>(Column);

            if (string.IsNullOrWhiteSpace(value) || !value.IsJson())
            {
                return default;
            }

            var jToken = JToken.Parse(value).SelectToken(Path);
            switch (jToken)
            {
                case null: return default;
                case JValue jValue: return jValue.Value;
                default:
                    if (Strict)
                    {
                        throw DynamicException.Create(
                            $"{Name}JsonPath",
                            $"Expected {nameof(JValue)} but found {jToken.GetType().Name}. {nameof(Path)} must select a single value from {Column}. "
                        );
                    }
                    return default;
            }
        }
    }
}