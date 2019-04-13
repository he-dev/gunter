using System;
using System.Collections.Generic;
using System.Linq;
using Gunter.Data;
using Reusable;

namespace Gunter.Services
{
    internal static class RuntimeVariableExtensions
    {
        public static IEnumerable<KeyValuePair<SoftString, object>> GetValues(this IEnumerable<IRuntimeVariable> variables, object obj)
        {
            // Static variables are resolved by the declaring type.
            return
                variables
                    .Where(variable => variable.Matches(obj is Type type ? type : obj.GetType()))
                    .Select(x => new KeyValuePair<SoftString, object>(x.Name, x.GetValue(obj)));
        }
    }
}