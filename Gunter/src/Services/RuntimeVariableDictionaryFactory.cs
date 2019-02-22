using System.Collections.Generic;
using System.Linq;
using Gunter.Data;
using JetBrains.Annotations;
using Reusable;

namespace Gunter.Services
{
    [UsedImplicitly]
    public class RuntimeVariableDictionaryFactory
    {
        private readonly IEnumerable<object> _globalObjects;
        private readonly IEnumerable<IRuntimeValue> _runtimeVariables;

        public RuntimeVariableDictionaryFactory
        (
            ProgramInfo programInfo,
            IEnumerable<IRuntimeValue> runtimeVariables
        )
        {
            _globalObjects = new object[] { programInfo };
            _runtimeVariables = runtimeVariables;
        }

        public RuntimeVariableDictionary Create
        (
            IEnumerable<object> runtimeObjects,
            IEnumerable<TestBundleVariable> variables
        )
        {
            var dictionary =
                _globalObjects
                    .Concat(runtimeObjects)
                    .Select(_runtimeVariables.GetValues)
                    .SelectMany(x => x)
                    .Concat(variables.Select(x => (KeyValuePair<SoftString, object>)x))
                    // We pick the last variable from each group to allow variable override.
                    .GroupBy(x => x.Key)
                    .Select(x => x.Last())
                    .ToDictionary(x => x.Key, x => x.Value);

            return new RuntimeVariableDictionary(dictionary);
        }
    }
}