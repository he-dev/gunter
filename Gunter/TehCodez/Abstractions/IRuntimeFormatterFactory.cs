using System.Collections.Generic;
using Reusable;

namespace Gunter
{
    public interface IRuntimeFormatterFactory
    {
        IRuntimeFormatter Create(IDictionary<SoftString, object> locals, params object[] args);
    }
}