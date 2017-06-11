using System.Collections.Generic;
using System.Collections.Immutable;
using System;
using System.Collections;
using System.Linq;
using Reusable.Extensions;

namespace Gunter.Services
{
    public interface IVariableResolver : IEnumerable<KeyValuePair<string, object>>
    {
        int Count { get; }
        bool ContainsKey(string key);
        IVariableResolver Add(string key, object value);
        IVariableResolver MergeWith(IEnumerable<KeyValuePair<string, object>> other);
        string Resolve(string text);
    }

    public class VariableResolver : IVariableResolver
    {
        private readonly IDictionary<string, object> _variables;

        private VariableResolver()
        {
            _variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        private VariableResolver(IEnumerable<KeyValuePair<string, object>> variables)
        {
            _variables = variables.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        }

        public static IVariableResolver Empty => new VariableResolver();

        public int Count => _variables.Count;

        public bool ContainsKey(string key) => _variables.ContainsKey(key);

        public IVariableResolver Add(string key, object value)
        {
            return new VariableResolver(_variables.Concat(new[] { new KeyValuePair<string, object>(key, value) }));
        }

        public IVariableResolver MergeWith(IEnumerable<KeyValuePair<string, object>> other)
        {
            var result = other.Aggregate(new VariableResolver(_variables), (current, item) =>
            {
                current._variables[item.Key] = item.Value;
                return current;
            });
            return result;
        }

        public string Resolve(string text) => text.FormatAll(_variables);

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _variables.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    //internal class ComputedVariable
    //{
    //    private readonly Lazy<string> _getValue;

    //    public ComputedVariable(Func<string> getValue) => _getValue = new Lazy<string>(getValue);

    //    public override string ToString() => _getValue.Value;
    //}

    public interface IResolvable
    {
        IVariableResolver Variables { get; set; }
    }

    public static class ResolvableExtensions
    {
        public static T UpdateVariables<T>(this T resolvable, IVariableResolver variables) where T : class, IResolvable
        {
            resolvable.Variables = variables;
            return resolvable;
        }

        //public static IEnumerable<T> UpdateVariables<T>(this IEnumerable<T> resolvables, IVariableResolver variables) where T : class, IResolvable
        //{
        //    foreach (var resolvable in resolvables)
        //    {
        //        resolvable.Variables = variables;
        //    }
        //    return resolvables;
        //}
    }
}
