using System;
using System.Linq.Expressions;
using Gunter.Data.Abstractions;
using Gunter.Helpers;
using Gunter.Services.Abstractions;

namespace Gunter.Data.Properties
{
    internal class InstanceProperty<T> : RuntimeProperty
    {
        private readonly Func<object?> _getValue;

        public delegate InstanceProperty<T> Factory(Expression<Func<T, object?>> selectorExpression);

        public InstanceProperty(T instance, Expression<Func<T, object?>> selectorExpression, IMergeScalar mergeScalar) : base(CreateName(selectorExpression))
        {
            var getValue = selectorExpression.Compile();
            
            if (instance is IMergeable m)
            {
                _getValue = () => m.Resolve(x => getValue((T)x), mergeScalar);
            }
            else
            {
                _getValue = () => getValue(instance);
            }
        }

        public override object? GetValue() => _getValue();
    }
}