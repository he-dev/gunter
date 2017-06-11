using Reusable;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System;
using System.Collections;
using System.Linq;
using Gunter.Data;
using Reusable.Extensions;

// ReSharper disable UseStringInterpolation

namespace Gunter.Services
{
    public interface IVariableResolver : IEnumerable<KeyValuePair<string, object>>
    {
        string Resolve(string text);
        IVariableResolver UnionWith(IEnumerable<KeyValuePair<string, object>> other);
        IVariableResolver Add(string name, object value);
        IVariableResolver Add(string name, Func<string> getValue);
        bool ContainsKey(string name);
        bool TryGetValue(string key, out object value);
    }

    public class VariableResolver : IVariableResolver
    {
        private readonly IImmutableDictionary<string, object> _variables;

        private VariableResolver()
        {
            _variables = ImmutableDictionary.Create<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        private VariableResolver(IImmutableDictionary<string, object> variables)
        {
            _variables = variables;
        }

        public static IVariableResolver Empty => new VariableResolver();

        public string Resolve(string text) => text.FormatAll(_variables.ToDictionary(x => x.Key, x => x.Value));

        public IVariableResolver UnionWith(IEnumerable<KeyValuePair<string, object>> other)
        {
            var variables = other.Aggregate(_variables, (current, item) => current.SetItem(item.Key, item.Value));
            return new VariableResolver(variables);
        }

        public IVariableResolver Add(string name, object value) => new VariableResolver(_variables.Add(name, value));

        public IVariableResolver Add(string name, Func<string> getValue) => new VariableResolver(_variables.Add(name, new ComputedVariable(getValue)));

        public bool ContainsKey(string key) => !string.IsNullOrEmpty(key) && _variables.ContainsKey(key);

        public bool TryGetValue(string key, out object value) => _variables.TryGetValue(key, out value);

        #region IEnumerable

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _variables.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }

    internal class ComputedVariable
    {
        private readonly Lazy<string> _getValue;

        public ComputedVariable(Func<string> getValue) => _getValue = new Lazy<string>(getValue);

        public override string ToString() => _getValue.Value;
    }

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

        public static IEnumerable<T> UpdateVariables<T>(this IEnumerable<T> resolvables, IVariableResolver variables) where T : class, IResolvable
        {
            foreach (var resolvable in resolvables)
            {
                resolvable.Variables = variables;
            }
            return resolvables;
        }
    }
}
