using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Reusable;
using Reusable.Collections;
using Reusable.Reflection;

namespace Gunter.Data
{
    public interface IRuntimeValue : IEquatable<IRuntimeValue>
    {
        [AutoEqualityProperty]
        Type DeclaringType { get; }

        [AutoEqualityProperty]
        SoftString Name { get; }

        object Get<T>(T obj);

        bool Matches(Type type);

        string ToString(string format);
    }

    internal partial class RuntimeValue : IRuntimeValue
    {
        private readonly Func<object, object> _get;

        public RuntimeValue(Type declaringType, [NotNull] string name, [NotNull] Func<object, object> get)
        {
            Name = name;
            DeclaringType = declaringType;
            _get = get;
        }

        public Type DeclaringType { get; }

        public SoftString Name { get; }

        public object Get<T>(T obj)
        {
            return _get(obj);
        }

        public bool Matches(Type type)
        {
            return type == DeclaringType || type.IsAssignableFrom(DeclaringType);
        }

        #region IEquatable

        public bool Equals(IRuntimeValue other) => AutoEquality<IRuntimeValue>.Comparer.Equals(this, other);

        public override bool Equals(object other) => other is IRuntimeValue runtimeVariable && Equals(runtimeVariable);

        public override int GetHashCode() => AutoEquality<IRuntimeValue>.Comparer.GetHashCode(this);

        //public string ToString(string format, IFormatProvider formatProvider)
        //{
        //    throw new NotImplementedException();
        //}

        #endregion

        public override string ToString()
        {
            return "{" + Name.ToString() + "}";
        }

        public string ToString(string format)
        {
            return $"{{{Name.ToString()}:{format}}}";
        }
    }
}