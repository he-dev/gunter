using System;
using System.Collections.Generic;
using System.Linq;
using Gunter.Data;
using JetBrains.Annotations;
using Reusable;
using Reusable.Extensions;

namespace Gunter
{
    public delegate string FormatFunc(string text);

    public interface IRuntimeFormatter
    {
        string Format(string text);

        IRuntimeFormatter AddRange(IEnumerable<KeyValuePair<SoftString, object>> variables);
    }

    [UsedImplicitly]
    public class RuntimeFormatter : IRuntimeFormatter
    {
        private readonly IEnumerable<IRuntimeVariable> _runtimeVariables;

        private readonly IDictionary<SoftString, object> _variables;

        public RuntimeFormatter(IEnumerable<IRuntimeVariable> runtimeVariables)
        {
            _runtimeVariables = runtimeVariables.ToList();
            _variables = new Dictionary<SoftString, object>();
        }

        private RuntimeFormatter(IEnumerable<IRuntimeVariable> runtimeVariables, IEnumerable<KeyValuePair<SoftString, object>> variables)
        {
            _runtimeVariables = runtimeVariables;
            _variables = variables.ToDictionary(x => x.Key, x => x.Value);
        }

        public string Format(string text) => text.FormatAll(_variables);

        public IRuntimeFormatter AddRange(IEnumerable<KeyValuePair<SoftString, object>> variables)
        {
            return new RuntimeFormatter(_runtimeVariables, _variables.Concat(variables).GroupBy(x => x.Key).Select(x => x.Last()));
        }
    }
}
