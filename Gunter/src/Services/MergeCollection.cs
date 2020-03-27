using System;
using System.Collections.Generic;
using System.Linq;
using Gunter.Data.Abstractions;
using Gunter.Data.Configuration;
using Gunter.Services.Abstractions;

namespace Gunter.Services
{
    public class MergeCollection : Merge, IMergeCollection
    {
        public MergeCollection(IEnumerable<Theory> templates) : base(templates) { }

        public virtual IEnumerable<TValue> Execute<T, TValue, TKey>(T mergeable, Func<T, IEnumerable<TValue>> getValue, Func<TValue, TKey> keySelector, IEqualityComparer<TKey>? comparer = default) where T : IModel, IMergeable
        {
            return
                FindModels(mergeable)
                    .Select(getValue)
                    .Prepend(getValue(mergeable))
                    .SelectMany(x => x)
                    .GroupBy(keySelector, comparer ?? EqualityComparer<TKey>.Default)
                    .Select(g => g.First());
        }
    }
}