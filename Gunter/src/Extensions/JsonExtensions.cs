namespace Gunter.Extensions
{
    internal static class JsonExtensions
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