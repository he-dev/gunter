using System;
using System.Collections.Generic;
using System.Linq;
using Gunter.Data.Abstractions;

namespace Gunter.Services
{
    public class GetHandlers : List<IServiceMapping>
    {
        public IEnumerable<Type> For(object handlee)
        {
            return
                from m in this
                where m.HandleeType.IsInstanceOfType(handlee)
                select m.HandlerType;
        }
    }
}