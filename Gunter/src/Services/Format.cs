using System;
using System.Collections.Generic;
using System.Linq;
using Gunter.Data.Abstractions;
using Reusable.Extensions;

namespace Gunter.Services
{
    public class Format
    {
        public Format(IEnumerable<IProperty> properties)
        {
            Properties = properties;
        }

        private IEnumerable<IProperty> Properties { get; }

        public virtual string? Execute(string value)
        {
            return value.Format(name => FindProperty(name)?.GetValue()?.ToString());
        }

        private IProperty? FindProperty(string name)
        {
            return Properties.FirstOrDefault(p => p.Name.Equals(name));
        }
        

        public static implicit operator Func<string, string?>(Format format) => format.Execute;
    }
}