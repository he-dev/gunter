using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Reusable;
using Reusable.Collections;
using Reusable.Diagnostics;

namespace Gunter.Data.Abstractions
{
    [PublicAPI]
    [UsedImplicitly]
    public interface IProperty : IEquatable<IProperty>
    {
        [AutoEqualityProperty]
        SoftString Name { get; }

        object? GetValue();
    }

    [DebuggerDisplay(DebuggerDisplayString.DefaultNoQuotes)]
    public abstract class RuntimeProperty : IProperty
    {
        protected RuntimeProperty(string name) => Name = name;

        private string DebuggerDisplay => this.ToDebuggerDisplayString(builder =>
        {
            builder.DisplaySingle(x => x.Name);
            builder.DisplaySingle(x => x.GetValue());
        });

        public SoftString Name { get; }

        public abstract object? GetValue();

        #region IEquatable

        public bool Equals(IProperty other) => AutoEquality<IProperty>.Comparer.Equals(this, other);

        public override bool Equals(object? other) => other is IProperty runtimeVariable && Equals(runtimeVariable);

        public override int GetHashCode() => AutoEquality<IProperty>.Comparer.GetHashCode(this);

        #endregion

        public override string ToString() => DebuggerDisplay;

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

    internal static class PropertyExtensions
    {
        public static string ToFormatString(this IProperty property, string format)
        {
            return $"{{{property.Name}:{format}}}";
        }

        public static string ToPlaceholder(this IProperty property)
        {
            return "{" + property.Name + "}";
        }
    }
}