using Gunter.Data;
using Reusable.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gunter.Services.Validators
{
    internal static class VariableValidator
    {
        public static void ValidateNamesNotReserved(IVariableResolver variables)
        {
            var reservedNames = VariableName.GetReservedNames().ToList();
            if (reservedNames.Any(variables.ContainsKey))
            {
                throw new ReservedVariableNameException(reservedNames);
            }
        }
    }
}
