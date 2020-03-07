using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Gunter.Data;
using JetBrains.Annotations;
using Reusable;
using Reusable.Extensions;

namespace Gunter.Services
{
    [PublicAPI]
    public class RuntimePropertyProvider
    {
        private readonly IImmutableList<IProperty> _properties;
        private readonly IImmutableList<object> _objects;

        public RuntimePropertyProvider
        (
            IImmutableList<IProperty> knownProperties,
            IImmutableList<object> objects
        )
        {
            _properties = knownProperties;
            _objects = objects;
        }

        public bool TryGetValue(SoftString key, out object value)
        {
            var property = _properties.SingleOrDefault(p => p.Name == key);

            switch (property)
            {
                case null:
                    value = default;
                    return false;
                
                case StaticProperty staticProperty:
                    value = staticProperty.GetValue(default);
                    return true;

                case InstanceProperty instanceProperty when instanceProperty.SourceType is {}:
                    if (_objects.SingleOrDefault(o => instanceProperty.SourceType.IsInstanceOfType(o)) is var obj && obj is null)
                    {
                        value = default;
                        return false;
                    }
                    else
                    {
                        value = property.GetValue(obj);
                        return true;
                    }
                default:
                    value = default;
                    return false;
            }
        }

        [DebuggerStepThrough]
        public RuntimePropertyProvider AddObject(object obj) => new RuntimePropertyProvider(_properties, _objects.Add(obj));

        [DebuggerStepThrough]
        public RuntimePropertyProvider AddProperty(IProperty property) => new RuntimePropertyProvider(_properties.Add(property), _objects);

        public static implicit operator TryGetValueCallback(RuntimePropertyProvider provider)
        {
            return (string name, out object value) => provider.TryGetValue(name, out value);
        }
    }
}