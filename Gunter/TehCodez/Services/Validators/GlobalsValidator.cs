using Gunter.Data;
using Reusable.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gunter.Services.Validators
{
    internal class GlobalsValidator
    {
        public static void ValidateNames(IConstantResolver globals, ILogger logger)
        {
            var reservedNames = VariableName.GetReservedNames();
            foreach(var reservedName in reservedNames.Where(reservedName => globals.ContainsKey(reservedName)))
            {
                LogEntry.New().Warn().Message($"Reserved name '{reservedName}' will be overwritten.").Log(logger);
            }
        }
    }
}
