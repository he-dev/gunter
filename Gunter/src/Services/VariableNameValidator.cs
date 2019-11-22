using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Custom;
using Gunter.Data;
using JetBrains.Annotations;
using Reusable;
using Reusable.Exceptionize;
using Reusable.Extensions;

namespace Gunter.Services
{
    public interface IRuntimePropertyNameValidator
    {
        void ValidateNamesNotReserved(IEnumerable<SoftString> variableNames);
    }

    [UsedImplicitly]
    internal class RuntimePropertyNameValidator : IRuntimePropertyNameValidator
    {
        private readonly IEnumerable<SoftString> _reservedNames;

        public RuntimePropertyNameValidator(IEnumerable<IProperty> runtimeVariables)
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
                throw DynamicException.Create
                (
                    $"ReservedVariableName{nameof(Exception)}",
                    $"These variable names use reserved names: {invalidVariableNames.Join(", ").EncloseWith("[]")}"
                );
            }
        }
    }
}