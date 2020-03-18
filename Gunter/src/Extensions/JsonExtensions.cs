namespace Gunter.Extensions
{
    internal static class JsonExtensions
    {
        public static bool IsJson(this string value)
        {
            return value.OpeningType() == value.ClosingType();
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
                    default: return JsonType.Invalid;
                }
            }

            return JsonType.Invalid;
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
                    default: return JsonType.Invalid;
                }
            }

            return JsonType.Invalid;
        }

        private enum JsonType
        {
            Invalid,
            Array,
            Object
        }
    }
}