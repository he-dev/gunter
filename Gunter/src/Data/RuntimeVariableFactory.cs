using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Reusable;

namespace Gunter.Data
{
    public class RuntimeVariableFactory
    {
        [NotNull]
        public static IRuntimeVariable Create<T>(Expression<Func<T, object>> expression)
        {
            var parameter = Expression.Parameter(typeof(object), "obj");
            var converted = ParameterConverter<T>.Convert(expression.Body, parameter);
            // ((T)obj).Property
            var getValueFunc = Expression.Lambda<Func<object, object>>(converted, parameter).Compile();

            return new RuntimeVariable(
                typeof(T),
                CreateName(expression),
                getValueFunc
            );
        }

        [NotNull]
        public static IRuntimeVariable Create(Expression<Func<object>> expression)
        {
            var memberExpression = (MemberExpression)expression.Body;

            return new RuntimeVariable(
                memberExpression.Member.DeclaringType,
                CreateName(expression),
                _ => expression.Compile()()
            );
        }

        private static string CreateName(Expression expression)
        {
            expression = expression is LambdaExpression lambda ? lambda.Body : expression;

            while (true)
            {
                switch (expression)
                {
                    case MemberExpression memberExpression:
                        // ReSharper disable once PossibleNullReferenceException
                        // For member expression the DeclaringType cannot be null.
                        var typeName = memberExpression.Member.DeclaringType.Name;
                        if (memberExpression.Member.DeclaringType.IsInterface)
                        {
                            // Remove the leading "I" from an interface name.
                            typeName = Regex.Replace(typeName, "^I", string.Empty);
                        }
                        return $"{typeName}.{memberExpression.Member.Name}";

                    // Value types are wrapped by Convert(x) which is an unary-expression.
                    case UnaryExpression unaryExpression:
                        expression = unaryExpression.Operand;
                        continue;
                }

                // There is an unary-expression when using interfaces.

                throw new ArgumentException("Member expression not found.");
            }
        }
    }

    internal static class RuntimeVariableExtensions
    {
        public static IEnumerable<KeyValuePair<SoftString, object>> GetValues(this IEnumerable<IRuntimeVariable> variables, object obj)
        {
            // Static variables are resolved by the declaring type.
            return
                variables
                    .Where(variable => variable.Matches(obj is Type type ? type : obj.GetType()))
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