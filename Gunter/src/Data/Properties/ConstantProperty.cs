using Gunter.Data.Abstractions;
using JetBrains.Annotations;

namespace Gunter.Data.Properties
{
    [PublicAPI]
    [UsedImplicitly]
    public class ConstantProperty : RuntimeProperty
    {
        private readonly object _value;

        public ConstantProperty(string name, object value) : base(name) => _value = value;

        public override object? GetValue() => _value;
    }
}