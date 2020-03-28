using System;
using System.Collections.Generic;
using Gunter.Data.Abstractions;
using Gunter.Services.Abstractions;

namespace Gunter.Helpers
{
    public static class MergeHelper
    {
        public static TValue Resolve<T, TValue>(this T mergeable, Func<T, TValue> getValue, IMergeScalar merge, Func<TValue, bool>? isValid = default) where T : IModel, IMergeable
        {
            return merge.Execute(mergeable, getValue, isValid ?? (_ => true));
        }

        public static IEnumerable<TValue> Resolve<T, TValue, TKey>
        (
            this T mergeable,
            Func<T, IEnumerable<TValue>> getValue,
            IMergeCollection merge,
            Func<TValue, TKey> keySelector,
            IEqualityComparer<TKey>? comparer = default
        ) where T : IModel, IMergeable
        {
            return merge.Execute(mergeable, getValue, keySelector, comparer);
        }
    }
}