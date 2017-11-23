using System.Collections.Generic;
using System.Collections.Immutable;
using System;
using System.Collections;
using System.Linq;
using JetBrains.Annotations;
using Reusable.Extensions;

namespace Gunter.Services
{
    public delegate string FormatFunc(string text);

    public interface IRuntimeFormatter
    {
        string Format(string text);
        IRuntimeFormatter AddRange(IEnumerable<KeyValuePair<string, object>> variables);
    }

    [UsedImplicitly]
    public class RuntimeFormatter : IRuntimeFormatter
    {
        private readonly IDictionary<string, object> _variables;

        private RuntimeFormatter(IEnumerable<KeyValuePair<string, object>> variables)
        {
            _variables = variables.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        }

        public string Format(string text) => text.FormatAll(_variables);

        public IRuntimeFormatter AddRange(IEnumerable<KeyValuePair<string, object>> variables)
        {
            return new RuntimeFormatter(_variables.Concat(variables).GroupBy(x => x.Key).Select(x => x.Last()));
        }
    }
}
