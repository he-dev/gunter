using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Reusable;
using Reusable.Collections;

namespace Gunter.Data
{
    public interface IRuntimeVariable : IEquatable<IRuntimeVariable>
    {
        [AutoEqualityProperty]
        Type DeclaringType { get; }

        [AutoEqualityProperty]
        SoftString Name { get; }

        object GetValue<T>(T obj);
    }

    internal class RuntimeVariable : IRuntimeVariable
    {
        private readonly Func<object, object> _getValue;

        private RuntimeVariable(Type declaringType, [NotNull] string name, [NotNull] Func<object, object> getValue)
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

        [NotNull]
        public static IRuntimeVariable FromExpression<T>(Expression<Func<T, object>> expression)
        {
            var parameter = Expression.Parameter(typeof(object), "obj");
            var converted = ParameterConverter<T>.Convert(expression.Body, parameter);
            var getValueFunc = Expression.Lambda<Func<object, object>>(converted, parameter).Compile();

            return new RuntimeVariable(typeof(T), CreateName(expression), getValueFunc);
        }

        private static string CreateName(Expression expression)
        {
            while (true)
            {
                expression = expression is LambdaExpression lambda ? lambda.Body : expression;

                if (expression is MemberExpression memberExpression)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    // For member expression the DeclaringType cannot be null.
                    var typeName = memberExpression.Member.DeclaringType.Name;
                    if (memberExpression.Member.DeclaringType.IsInterface)
                    {
                        // Remove the leading "I" from an interface name.
                        typeName = Regex.Replace(typeName, "^I", string.Empty);
                    }
                    return $"{typeName}.{memberExpression.Member.Name}";
                }

                // There is an unary-expression when using interfaces.
                if (expression is UnaryExpression unaryExpression)
                {
                    expression = unaryExpression.Operand;
                    continue;
                }

                throw new ArgumentException("Member expression not found.");
            }
        }

        public bool Equals(IRuntimeVariable other) => AutoEquality<IRuntimeVariable>.Comparer.Equals(this, other);

        public override bool Equals(object other) => other is IRuntimeVariable runtimeVariable && Equals(runtimeVariable);

        public override int GetHashCode() => AutoEquality<IRuntimeVariable>.Comparer.GetHashCode(this);
    }

    internal static class RuntimeVariableExtensions
    {
        public static IEnumerable<KeyValuePair<SoftString, object>> Resolve<T>(this IEnumerable<IRuntimeVariable> variables, T obj)
        {
            return
                variables
                    .Where(x => typeof(T).IsAssignableFrom(x.DeclaringType))
                    .Select(x => new KeyValuePair<SoftString, object>(x.Name, x.GetValue(obj)));
        }
    }

    internal class ParameterConverter<T> : ExpressionVisitor
    {
        private readonly ParameterExpression _parameter;

        private ParameterConverter(ParameterExpression parameter)
        {
            _parameter = parameter;
        }

        public static Expression Convert(Expression expression, ParameterExpression parameter)
        {
            return new ParameterConverter<T>(parameter).Visit(expression);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return Expression.Convert(_parameter, typeof(T));
        }
    }
}