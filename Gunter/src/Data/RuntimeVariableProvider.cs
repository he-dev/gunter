using System.Collections.Generic;
using JetBrains.Annotations;
using Reusable;
using Reusable.Extensions;

namespace Gunter.Data
{
    [PublicAPI]
    public class RuntimeVariableProvider
    {
        private readonly IDictionary<SoftString, object> _dictionary;

        public RuntimeVariableProvider(IDictionary<SoftString, object> dictionary)
        {
            _dictionary = dictionary;
        }

        public bool TryGetValue(SoftString key, out object value) => _dictionary.TryGetValue(key, out value);

        public static implicit operator TryGetValueCallback(RuntimeVariableProvider provider) => (string name, out object value) => provider.TryGetValue(name, out value);
    }
}