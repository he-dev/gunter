using System;
using System.Collections.Generic;
using System.Linq;

namespace Gunter.Extensions
{
    internal static class EnumberableExtensions
    {
        /// <summary>
        /// Aggregates throw if the collection is empty. Make sure it isn't before calculating.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="aggregate"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T AggregateOrDefault<T>(this IEnumerable<T> values, Func<IEnumerable<T>, T> aggregate, T defaultValue)
        {
            return values.Any() ? aggregate(values) : defaultValue;
        }        
    }
}