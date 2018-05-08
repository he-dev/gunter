﻿using System;
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
        void ValidateNamesNotReserved(IEnumerable<SoftString> variableNames);
    }

    [UsedImplicitly]
    internal class VariableNameValidator : IVariableNameValidator
    {
        private readonly IEnumerable<SoftString> _reservedNames;

        public VariableNameValidator(IEnumerable<IRuntimeVariable> runtimeVariables)
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