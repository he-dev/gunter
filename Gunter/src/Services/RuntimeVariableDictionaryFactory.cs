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
        private readonly IEnumerable<IRuntimeVariable> _runtimeVariables;

        public RuntimeVariableDictionaryFactory
        (
            ProgramInfo programInfo,
            IEnumerable<IRuntimeVariable> runtimeVariables
        )
        {
            _globalObjects = new object[] { programInfo };
            _runtimeVariables = runtimeVariables;
        }

        public RuntimeVariableProvider Create
        (
            IEnumerable<object> runtimeObjects,
            IEnumerable<TestBundleVariable> variables
        )
        {
            var tuples = variables.Select(x => (x.Name, x.Value));
            
            var dictionary =
                _globalObjects
                    .Concat(runtimeObjects)
                    .Select(_runtimeVariables.GetValues)
                    .SelectMany(x => x)
                    .Concat(tuples) // You need this helper variable because the Select didn't work here.
                    .GroupBy(x => x.Name)
                    .Select(x => x.Last()) // You pick the last variable from each group to allow variable override.
                    .ToDictionary(x => x.Name, x => x.Value);

            return new RuntimeVariableProvider(dictionary);
        }
    }
}