using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable;
using Reusable.Collections;

namespace Gunter.Data
{
    [PublicAPI]
    [UsedImplicitly]
    public interface IProperty : IEquatable<IProperty>
    {
        [AutoEqualityProperty]
        SoftString Name { get; }

        object? GetValue();
    }

    public abstract class RuntimeProperty : IProperty
    {
        protected RuntimeProperty(string name) => Name = name;

        public SoftString Name { get; }

        public abstract object? GetValue();

        #region IEquatable

        public bool Equals(IProperty other) => AutoEquality<IProperty>.Comparer.Equals(this, other);

        public override bool Equals(object other) => other is IProperty runtimeVariable && Equals(runtimeVariable);

        public override int GetHashCode() => AutoEquality<IProperty>.Comparer.GetHashCode(this);

        #endregion

        protected static string CreateName(Expression expression)
        {
            return expression switch
            {
                LambdaExpression lambdaExpression => CreateName(lambdaExpression.Body),
                MemberExpression memberExpression => CreateName(memberExpression),
                // Value types are wrapped by Convert(x) which is an unary-expression.
                UnaryExpression unaryExpression => CreateName(unaryExpression.Operand),
                // There is an unary-expression when using interfaces.
                _ => throw new ArgumentException("Member expression not found.")
            };
        }

        private static string CreateName(MemberExpression memberExpression)
        {
            // ReSharper disable once PossibleNullReferenceException - For member expression the DeclaringType cannot be null.
            var typeName = memberExpression.Member.ReflectedType.Name;
            if (memberExpression.Member.ReflectedType.IsInterface)
            {
                // Remove the leading "I" from an interface name.
                typeName = Regex.Replace(typeName, "^I", string.Empty);
            }

            return $"{typeName}.{memberExpression.Member.Name}";
        }
    }

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

    [PublicAPI]
    [UsedImplicitly]
    public class StaticProperty : RuntimeProperty
    {
        private readonly object _value;

        public StaticProperty(Expression<Func<object?>> selectorExpression) : base(CreateName(selectorExpression))
        {
            _value = selectorExpression.Compile()();
        }

        public override object? GetValue() => _value;

        //public static implicit operator StaticProperty(KeyValuePair<string, object> kvp) => new StaticProperty(kvp.Key, kvp.Value);

        //public static implicit operator KeyValuePair<SoftString, object>(StaticProperty tbv) => new KeyValuePair<SoftString, object>(tbv.Name, tbv.Value);
    }

    internal static class PropertyExtensions
    {
        public static string ToFormatString(this IProperty property, string format)
        {
            return $"{{{property.Name.ToString()}:{format}}}";
        }

        public static string ToPlaceholder(this IProperty property)
        {
            return "{" + property.Name.ToString() + "}";
        }
    }
}