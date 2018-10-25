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
            var value = source.Field<string>(JsonColumn);

            if (string.IsNullOrWhiteSpace(value) || !value.IsJson())
            {
                return default;
            }
            
            var jToken = JToken.Parse(value).SelectToken(JsonPath);
            switch (jToken)
            {
                case null: return default;
                case JValue jValue: return jValue.Value;
                default: throw DynamicException.Create($"{Name}JsonPath", $"Expected {nameof(JValue)} but found {jToken.GetType().Name}. {nameof(JsonPath)} must select a single value from {JsonColumn}. ");
            }
        }
    }

    internal static class StringExtensions
    {
        public static bool IsJson(this string value)
        {
            return value.StartsWith(out var startsWith) && value.EndsWith(out var endsWith) && startsWith == endsWith;
        }

        private static bool StartsWith(this string value, out JsonType startsWith)
        {
            startsWith = JsonType.Invalid;

            for (var i = 0; i < value.Length; i++)
            {
                switch (value[i])
                {
                    case ' ': continue;
                    case '[': startsWith = JsonType.Array; return true;
                    case '{': startsWith = JsonType.Object; return true;
                    default: return false;
                }
            }

            return false;
        }

        private static bool EndsWith(this string value, out JsonType endsWith)
        {
            endsWith = JsonType.Invalid;

            for (var i = value.Length - 1; i >= 0; i--)
            {
                switch (value[i])
                {
                    case ' ': continue;
                    case ']': endsWith = JsonType.Array; return true;
                    case '}': endsWith = JsonType.Object; return true;
                    default: return false;
                }
            }

            return false;
        }

        private enum JsonType
        {
            Invalid,
            Array,
            Object
        }
    }
}