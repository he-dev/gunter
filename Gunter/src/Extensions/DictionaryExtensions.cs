using System.Collections.Generic;
using System.Linq;

namespace Gunter.Extensions
{
    internal static class DictionaryExtensions
    {
        public static void UnionWith<TKey, TValue>(this IDictionary<TKey, TValue> target, IEnumerable<KeyValuePair<TKey, TValue>> other)
        {
            foreach (var pair in other)
            {
                if (!target.ContainsKey(pair.Key))
                {
                    target.Add(pair);
                }
            }
        }

        public static IEnumerable<KeyValuePair<TKey, TValue>> Union<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> first, IEnumerable<KeyValuePair<TKey, TValue>> second)
        {
            return first.Concat(second).GroupBy(x => x.Key, (k, g) => g.First());
        }
    }
}