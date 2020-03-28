using System;
using System.Collections.Generic;
using System.Linq;
using Gunter.Data;
using Gunter.Data.Abstractions;
using Gunter.Data.Configuration;

namespace Gunter.Services.Abstractions
{
    public interface IMergeProvider
    {
        IMergeScalar Scalar { get; }
        
        IMergeCollection Collection { get; }
    }
    
    public interface IMergeScalar
    {
        TValue Execute<T, TValue>(T mergeable, Func<T, TValue> getValue, Func<TValue, bool> isValid) where T : IModel, IMergeable;
    }

    public interface IMergeCollection
    {
        IEnumerable<TValue> Execute<T, TValue, TKey>(T mergeable, Func<T, IEnumerable<TValue>> getValue, Func<TValue, TKey> keySelector, IEqualityComparer<TKey>? comparer = default) where T : IModel, IMergeable;
    }

    public abstract class Merge
    {
        protected Merge(IEnumerable<Theory> templates)
        {
            Templates = templates;
        }

        private IEnumerable<Theory> Templates { get; }

        protected IEnumerable<T> FindModels<T>(T mergeable) where T : IModel, IMergeable
        {
            return
                from t in Templates
                from m in t.OfType<T>()
                where mergeable.ModelSelector is {} modelSelector && ModelSelector.Comparer.Equals(new ModelSelector(t.Name, m.Name), modelSelector)
                select m;
        }
    }
}