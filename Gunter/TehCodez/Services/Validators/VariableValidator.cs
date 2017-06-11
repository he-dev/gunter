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
        public static void ValidateNoReservedNames(IVariableResolver variables)
        {
            var reservedNames = VariableName.GetReservedNames().ToList();
            if (reservedNames.Any(variables.ContainsKey))
            {
                throw new ReservedNameException(reservedNames);
            }
        }
    }
}
