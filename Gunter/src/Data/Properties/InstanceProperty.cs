using System;
using System.Linq.Expressions;
using Gunter.Data.Abstractions;

namespace Gunter.Data.Properties
{
    internal class InstanceProperty<T> : RuntimeProperty
    {
        private readonly T _instance;
        private readonly Func<T, object?> _getValue;

        public delegate InstanceProperty<T> Factory(Expression<Func<T, object?>> selectorExpression);

        public InstanceProperty(T instance, Expression<Func<T, object?>> selectorExpression) : base(CreateName(selectorExpression))
        {
            _instance = instance;
            _getValue = selectorExpression.Compile();
        }

        public override object? GetValue() => _getValue(_instance);
    }
}