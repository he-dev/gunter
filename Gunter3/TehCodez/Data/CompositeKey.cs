using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Gunter.Data
{
    public class CompositeKey<T> : IEnumerable<T>, IEquatable<CompositeKey<T>>
    {
        private readonly List<T> _keys;

        public CompositeKey(IEnumerable<T> keys) => _keys = new List<T>(keys);

        public IEnumerator<T> GetEnumerator() => _keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Equals(CompositeKey<T> other)
        {
            if (ReferenceEquals(other, null)) return false;
            return ReferenceEquals(this, other) || this.SequenceEqual(other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CompositeKey<T>)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return this.Aggregate(0, (current, next) => (current * 397) ^ next?.GetHashCode() ?? 0);
            }
        }
    }
}