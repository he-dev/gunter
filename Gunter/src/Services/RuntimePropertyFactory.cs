using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Gunter.Data;
using JetBrains.Annotations;

namespace Gunter.Services
{
    public static class RuntimePropertyFactory
    {
        [NotNull]
        public static IProperty Create<T>(Expression<Func<T, object>> getValueExpression)
        {
            var parameter = Expression.Parameter(typeof(object), "obj");
            var converted = RuntimePropertyConverter<T>.Convert(getValueExpression.Body, parameter);
            var getValueFunc = Expression.Lambda<Func<object, object>>(converted, parameter).Compile();
            
            return new InstanceProperty(typeof(T), CreateName(getValueExpression), getValueFunc);
        }

        [NotNull]
        public static IProperty Create(Expression<Func<object>> expression)
        {
            var memberExpression = (MemberExpression)expression.Body;

            return new InstanceProperty
            (
                memberExpression.Member.ReflectedType,
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
                        var typeName = memberExpression.Member.ReflectedType.Name;
                        if (memberExpression.Member.ReflectedType.IsInterface)
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

    // (T)obj
    internal class RuntimePropertyConverter<T> : ExpressionVisitor
    {
        private readonly ParameterExpression _parameter;

        private RuntimePropertyConverter(ParameterExpression parameter)
        {
            _parameter = parameter;
        }

        public static Expression Convert(Expression expression, ParameterExpression parameter)
        {
            return new RuntimePropertyConverter<T>(parameter).Visit(expression);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return Expression.Convert(_parameter, typeof(T));
        }
    }
}