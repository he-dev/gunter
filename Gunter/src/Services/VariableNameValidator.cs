using System;
using System.Collections.Generic;
using System.Linq;
using Gunter.Data;
using JetBrains.Annotations;
using Reusable;
using Reusable.Exceptionize;
using System.Linq.Custom;
using Reusable.Extensions;

namespace Gunter
{
    public interface IVariableNameValidator
    {
        void ValidateNamesNotReserved(IDictionary<SoftString, object> variables);
    }

    [UsedImplicitly]
    internal class VariableNameValidator : IVariableNameValidator
    {
        private readonly IEnumerable<SoftString> _reservedNames;

        public VariableNameValidator(IEnumerable<IRuntimeVariable> runtimeVariables)
        {
            _reservedNames = runtimeVariables.Select(x => x.Name).ToList();
        }

        public void ValidateNamesNotReserved(IDictionary<SoftString, object> variables)
        {
            var invalidVariableNames = variables.Keys.Intersect(_reservedNames).ToList();
            if (invalidVariableNames.Any())
            {
                throw DynamicException.Factory.CreateDynamicException(
                    $"ReservedVariableName{nameof(Exception)}",
                    $"Some variables use reserved names: {invalidVariableNames.Join(", ").EncloseWith("[]")}",
                    null
                );
            }
        }
    }    
}
