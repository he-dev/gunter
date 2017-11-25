using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reusable;
using Reusable.Extensions;

namespace Gunter
{
    public delegate string FormatFunc(string text);

    [UsedImplicitly]
    public class RuntimeFormatter : IRuntimeFormatter
    {
        private readonly IDictionary<SoftString, object> _variables;

        public RuntimeFormatter(IEnumerable<KeyValuePair<SoftString, object>> variables)
        {
            _variables = 
                variables
                    // We pick the last variable from each group to allow variable override.
                    .GroupBy(x => x.Key)
                    .Select(x => x.Last())
                    .ToDictionary(x => x.Key, x => x.Value);
        }

        public string Format(string text) => text.FormatAll(_variables);       
    }
}
