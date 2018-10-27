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
    public interface IRuntimeVariable : IEquatable<IRuntimeVariable>//, IFormattable
    {
        [AutoEqualityProperty]
        Type DeclaringType { get; }

        [AutoEqualityProperty]
        SoftString Name { get; }

        object GetValue<T>(T obj);

        bool Matches(Type type);

        string ToString(string format);
    }

    internal partial class RuntimeVariable : IRuntimeVariable
    {
        private readonly Func<object, object> _getValue;

        public RuntimeVariable(Type declaringType, [NotNull] string name, [NotNull] Func<object, object> getValue)
        {
            Name = name;
            DeclaringType = declaringType;
            _getValue = getValue;
        }

        public Type DeclaringType { get; }

        public SoftString Name { get; }

        public object GetValue<T>(T obj)
        {
            return _getValue(obj);
        }

        public bool Matches(Type type)
        {
            return type == DeclaringType || type.IsAssignableFrom(DeclaringType);
        }

        #region IEquatable

        public bool Equals(IRuntimeVariable other) => AutoEquality<IRuntimeVariable>.Comparer.Equals(this, other);

        public override bool Equals(object other) => other is IRuntimeVariable runtimeVariable && Equals(runtimeVariable);

        public override int GetHashCode() => AutoEquality<IRuntimeVariable>.Comparer.GetHashCode(this);

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