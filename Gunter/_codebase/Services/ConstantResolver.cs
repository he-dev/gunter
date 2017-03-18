using Reusable;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System;
using System.Collections;
using System.Linq;
using Gunter.Data;

// ReSharper disable UseStringInterpolation

namespace Gunter.Services
{
    public interface IConstantResolver : IEnumerable<KeyValuePair<string, object>>
    {
        string Resolve(string text);
        IConstantResolver UnionWith(IEnumerable<KeyValuePair<string, object>> other);
        IConstantResolver Add(string name, object value);
        bool ContainsKey(string name);
        bool TryGetValue(string key, out object value);
    }

    public class ConstantResolver : IConstantResolver
    {
        private readonly ImmutableDictionary<string, object> _constants;

        public ConstantResolver(IEnumerable<KeyValuePair<string, object>> constants)
        {            
            _constants = constants.ToImmutableDictionary();            
        }

        public static IConstantResolver Empty => new ConstantResolver(ImmutableDictionary<string, object>.Empty);

        public string Resolve(string text) => text.FormatAll(_constants);

        public IConstantResolver UnionWith(IEnumerable<KeyValuePair<string, object>> other)
        {
            var result = new Dictionary<string, object>(_constants);
            foreach (var item in other) result[item.Key] = item.Value;
            return new ConstantResolver(result);
        }

        public IConstantResolver Add(string name, object value) => new ConstantResolver(_constants.Add(name, value));

        public bool ContainsKey(string key) => !string.IsNullOrEmpty(key) && _constants.ContainsKey(key);

        public bool TryGetValue(string key, out object value) => _constants.TryGetValue(key, out value);

        #region IEnumerable

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _constants.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


        #endregion
    }   
}
