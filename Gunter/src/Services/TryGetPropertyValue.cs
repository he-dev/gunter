using System.Collections.Generic;
using System.Linq;
using Gunter.Data.Abstractions;
using Reusable;
using Reusable.Extensions;

namespace Gunter.Services
{
    public class TryGetPropertyValue : ITryGetFormatValue
    {
        public TryGetPropertyValue(IEnumerable<IProperty> properties) => Properties = properties.ToDictionary(x => x.Name);

        private IDictionary<SoftString, IProperty> Properties { get; }

        public virtual bool Execute(string name, out object? value)
        {
            if (Properties.TryGetValue(name, out var property))
            {
                return (value = property.GetValue()) is {};
            }

            value = default;
            return false;
        }
    }
}