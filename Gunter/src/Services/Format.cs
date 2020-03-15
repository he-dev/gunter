using System;
using System.Collections.Generic;
using System.Linq;
using Gunter.Data;
using Reusable.Extensions;

namespace Gunter.Services
{
    public class Format
    {
        public Format(IEnumerable<IProperty> runtimeProperties)
        {
            RuntimeProperties = runtimeProperties;
        }

        private IEnumerable<IProperty> RuntimeProperties { get; }

        public virtual string Execute(string value)
        {
            return value.Format(name => (string?)RuntimeProperties.FirstOrDefault(p => p.Name.Equals(name))?.GetValue());
        }

        public static implicit operator Func<string, string>(Format format) => format.Execute;
    }
}