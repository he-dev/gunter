using System;
using System.Linq.Expressions;
using Gunter.Data.Abstractions;
using JetBrains.Annotations;

namespace Gunter.Data.Properties
{
    [PublicAPI]
    [UsedImplicitly]
    public class StaticProperty : RuntimeProperty
    {
        private readonly object _value;

        public StaticProperty(string name, object value) : base(name) => _value = value;

        public static IProperty For(Expression<Func<object?>> selectorExpression) => new StaticProperty
        (
            CreateName(selectorExpression),
            selectorExpression.Compile()()
        );

        public override object? GetValue() => _value;
    }
}