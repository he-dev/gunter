using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Custom;
using Gunter.Data.Configuration;

namespace Gunter.Services
{
    public class Merge
    {
        public Merge(Format format, IEnumerable<Theory> templates)
        {
            Format = format;
            Templates = templates;
        }

        private Format Format { get; }

        private IEnumerable<Theory> Templates { get; }

        public virtual TValue Execute<T, TValue>(T instance, Func<T, TValue> getValue)
        {
            var models = Templates.Flatten().OfType<T>();
            var values = models.Select(getValue).Prepend(getValue(instance));

            foreach (var value in values)
            {
                if (value is {})
                {
                    return value switch
                    {
                        string s => (TValue)(object)Format.Execute(s),
                        _ => value
                    };
                }
            }

            return default;
        }
    }
    
    public static class MergeHelper
    {
        public static IMerge<TValue> Merge<T, TValue>(this T instance, Func<T, TValue> getValue) => new Merge<T, TValue>
        {
            Instance = instance,
            GetValue = getValue
        };
    }

    public interface IMerge<out TValue>
    {
        TValue With(Merge merge);
    }

    public class Merge<T, TValue> : IMerge<TValue>
    {
        public T Instance { get; set; }

        public Func<T, TValue> GetValue { get; set; }

        public TValue With(Merge merge) => merge.Execute(Instance, GetValue);
    }
}