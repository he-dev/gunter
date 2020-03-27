using System;
using System.Collections.Generic;
using System.Linq;
using Gunter.Data.Abstractions;
using Gunter.Data.Configuration;
using Gunter.Services.Abstractions;

namespace Gunter.Services
{
    public class MergeScalar : Merge, IMergeScalar
    {
        public MergeScalar(IEnumerable<Theory> templates) : base(templates) { }

        public virtual TValue Execute<T, TValue>(T mergeable, Func<T, TValue> getValue) where T : IModel, IMergeable
        {
            return
                FindModels(mergeable)
                    .Select(getValue)
                    .Prepend(getValue(mergeable))
                    .FirstOrDefault(x => x is {});
        }
    }
}