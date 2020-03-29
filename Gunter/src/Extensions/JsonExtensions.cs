using System.Linq;
using System.Linq.Custom;

namespace Gunter.Extensions
{
    internal static class JsonExtensions
    {
        public static bool IsJson(this string value)
        {
            var openingType = value.OpeningType();
            var closingType = value.ClosingType();
            return (openingType == closingType) && new[] { openingType, closingType }.All(t => t.In(JsonType.Object, JsonType.Array));
        }

        private static JsonType OpeningType(this string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                switch (value[i])
                {
                    case '\r':
                    case '\n':
                    case ' ': continue;
                    case '[': return JsonType.Array;
                    case '{': return JsonType.Object;
                    default: return JsonType.Unknown;
                }
            }

            return JsonType.Unknown;
        }

        private static JsonType ClosingType(this string value)
        {
            for (var i = value.Length - 1; i >= 0; i--)
            {
                switch (value[i])
                {
                    case '\r':
                    case '\n':
                    case ' ': continue;
                    case ']': return JsonType.Array;
                    case '}': return JsonType.Object;
                    default: return JsonType.Unknown;
                }
            }

            return JsonType.Unknown;
        }

        private enum JsonType
        {
            Unknown,
            Array,
            Object
        }
    }
}