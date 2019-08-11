using System.Collections.Generic;
using System.Collections.Immutable;
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
        private readonly IImmutableList<object> _objects;
        private readonly IImmutableList<IProperty> _properties;

        public RuntimePropertyProvider
        (
            IEnumerable<IProperty> knownProperties,
            ProgramInfo programInfo
        )
        {
            _properties = ImmutableList<IProperty>.Empty.AddRange(knownProperties);
            _objects = ImmutableList<object>.Empty.Add(programInfo);
        }

        private RuntimePropertyProvider
        (
            IImmutableList<IProperty> properties,
            IImmutableList<object> objects
        )
        {
            _properties = properties;
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

                case InstanceProperty instanceProperty when !(instanceProperty.ObjectType is null):
                    if (_objects.SingleOrDefault(o => instanceProperty.ObjectType.IsInstanceOfType(o)) is var obj && obj is null)
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

        public RuntimePropertyProvider AddObjects(IEnumerable<object> objects)
        {
            return new RuntimePropertyProvider
            (
                _properties,
                _objects.AddRange(objects)
            );
        }

        public RuntimePropertyProvider AddObjects(params object[] objects) => AddObjects(objects.AsEnumerable());

        public RuntimePropertyProvider AddProperties(IEnumerable<IProperty> properties)
        {
            return new RuntimePropertyProvider
            (
                _properties.AddRange(properties),
                _objects
            );
        }

        public static implicit operator TryGetValueCallback(RuntimePropertyProvider provider)
        {
            return (string name, out object value) => provider.TryGetValue(name, out value);
        }
    }
}