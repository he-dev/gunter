using System.Collections.Generic;

namespace Gunter.Extensions
{
    internal static class DictionaryExtensions
    {
        public static Dictionary<TKey, TValue> UnionWith<TKey, TValue>(this Dictionary<TKey, TValue> source, Dictionary<TKey, TValue> other)
        {
            var result = new Dictionary<TKey, TValue>(source);
            foreach (var item in other)
            {
                result[item.Key] = item.Value;
            }
            return result;
        }
    }
}
