using System;
using System.Collections.Generic;
using System.Linq;
using Reusable.Collections;

namespace Gunter.Data
{
    public class DataRowKey : List<object>, IEquatable<DataRowKey>
    {
        public DataRowKey(IEnumerable<object> keys) : base(keys) { }

        public override int GetHashCode() => 0;

        public override bool Equals(object obj) => Equals(obj as DataRowKey);

        public bool Equals(DataRowKey? other) => Comparer.Equals(this, other);

        private static readonly IEqualityComparer<DataRowKey> Comparer = EqualityComparerFactory<DataRowKey>.Create
        (
            getHashCode: (obj) => 0,
            equals: (left, right) => left.SequenceEqual(right)
        );
    }
}