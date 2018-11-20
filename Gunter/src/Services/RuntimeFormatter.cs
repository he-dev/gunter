using System.Collections.Generic;
using System.Linq;
using System.Linq.Custom;
using Gunter.Data;
using JetBrains.Annotations;
using Reusable;
using Reusable.Extensions;

namespace Gunter.Services
{
    public delegate string FormatFunc(string text);

    public interface IRuntimeFormatter
    {
        string Format(string text);
    }

    [UsedImplicitly]
    public class RuntimeFormatter : IRuntimeFormatter
    {
        private readonly IDictionary<SoftString, object> _variables;

        public delegate RuntimeFormatter Factory(
            IEnumerable<KeyValuePair<SoftString, object>> variables,
            IEnumerable<object> runtimeObjects
        );

        public RuntimeFormatter(
            ProgramInfo programInfo,
            IEnumerable<IRuntimeVariable> runtimeVariables,
            IEnumerable<KeyValuePair<SoftString, object>> variables,
            IEnumerable<object> runtimeObjects
        )
        {
            _variables =
                runtimeObjects
                    .Append(programInfo)
                    .Select(runtimeVariables.GetValues)
                    .SelectMany(x => x)
                    .Concat(variables)
                    // We pick the last variable from each group to allow variable override.
                    .GroupBy(x => x.Key)
                    .Select(x => x.Last())
                    .ToDictionary(x => x.Key, x => x.Value);
        }

        public string Format(string text) => text.Format((string key, out object value) => _variables.TryGetValue(key, out value));
    }
}
