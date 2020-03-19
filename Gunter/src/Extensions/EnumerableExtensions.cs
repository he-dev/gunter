using System;
using System.Collections.Generic;
using System.Linq;
using Gunter.Data.Configuration.Reporting;

namespace Gunter.Extensions
{
    using static ReduceType;
    
    internal static class EnumerableExtensions
    {
        /// <summary>
        /// Aggregates throw if the collection is empty. Make sure it isn't before calculating.
        /// </summary>
        public static T AggregateOrDefault<T>(this IEnumerable<T> values, Func<IEnumerable<T>, T> aggregate, T defaultValue)
        {
            // ReSharper disable PossibleMultipleEnumeration 
            return values.Any() ? aggregate(values) : defaultValue;
            // ReSharper restore PossibleMultipleEnumeration 
        }      
        
        public static object? Reduce(this IEnumerable<object?> values,  ReduceType reduceType)
        {
            values = values.Where(x => x is {});
            
            return reduceType switch
            {
                First => values.FirstOrDefault(),
                Last => values.LastOrDefault(),
                Min => values.Select(Convert.ToDouble).AggregateOrDefault(Enumerable.Min, double.NaN),
                Max => values.Select(Convert.ToDouble).AggregateOrDefault(Enumerable.Max, double.NaN),
                Count => values.Count(),
                Sum => values.Select(Convert.ToDouble).Sum(),
                Average => values.Select(Convert.ToDouble).AggregateOrDefault(Enumerable.Average, double.NaN),
            };
        }
    }
}