using System.Collections.Generic;
using Reusable;

namespace Gunter
{
    public interface IVariableValidator
    {
        void ValidateNamesNotReserved(IDictionary<SoftString, object> variables);
    }
}