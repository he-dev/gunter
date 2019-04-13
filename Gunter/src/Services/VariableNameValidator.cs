using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Custom;
using Gunter.Data;
using JetBrains.Annotations;
using Reusable;
using Reusable.Exceptionize;
using Reusable.Extensions;
using Reusable.Reflection;

namespace Gunter.Services
{
    public interface IVariableNameValidator
    {
        void ValidateNamesNotReserved(IEnumerable<SoftString> variableNames);
    }

    [UsedImplicitly]
    internal class VariableNameValidator : IVariableNameValidator
    {
        private readonly IEnumerable<SoftString> _reservedNames;

        public VariableNameValidator(IEnumerable<IRuntimeValue> runtimeVariables)
        {
            _reservedNames = 
                runtimeVariables
                    .Select(x => x.Name)
                    .ToList();
        }

        public void ValidateNamesNotReserved(IEnumerable<SoftString> variableNames)
        {
            var invalidVariableNames = variableNames.Intersect(_reservedNames).ToList();
            if (invalidVariableNames.Any())
            {
                throw DynamicException.Factory.CreateDynamicException(
                    $"ReservedVariableName{nameof(Exception)}",
                    $"These variable names use reserved names: {invalidVariableNames.Join(", ").EncloseWith("[]")}",
                    null
                );
            }
        }
    }    
}
