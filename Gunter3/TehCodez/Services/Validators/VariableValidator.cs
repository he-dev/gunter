using Gunter.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gunter.Services.Validators
{
    internal static class VariableValidator
    {
        public static void ValidateNamesNotReserved(IDictionary<string, object> variables, IEnumerable<string> reservedNames)
        {
            if (reservedNames.Any(variables.ContainsKey))
            {
                throw new ReservedVariableNameException(reservedNames);
            }
        }
    }

    internal class ReservedVariableNameException : Exception
    {
        public ReservedVariableNameException(IEnumerable<string> names)
            : base($"You must not use any of these reserved names: [{string.Join(", ", names.Select(name => $"'{name}'"))}]")
        { }
    }
}
