using System.Collections.Generic;
using Reusable;

namespace Gunter.Services
{
    public class RuntimeVariableDictionary : Dictionary<SoftString, object>
    {
        public RuntimeVariableDictionary(IDictionary<SoftString, object> dictionary) : base(dictionary) { }
    }
}