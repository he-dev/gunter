using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Gunter.Data;
using JetBrains.Annotations;

namespace Gunter.Services
{
    public static class RuntimePropertyFactory
    {
        public static IProperty Create<T>(Expression<Func<T, object>> getValueExpression)
        {
            var parameter = Expression.Parameter(typeof(object), "obj");
            var converted = Cast<T>.Create(getValueExpression.Body, parameter);
            var getValueFunc = Expression.Lambda<Func<object, object>>(converted, parameter).Compile();

            return new InstanceProperty(CreateName(getValueExpression), typeof(T), getValueFunc);
        }

        public static IProperty Create(Expression<Func<object>> expression)
        {
            var memberExpression = (MemberExpression)expression.Body;

            return new InstanceProperty(CreateName(expression), memberExpression.Member.ReflectedType, _ => expression.Compile()());
        }

        
    }

    // (T)obj
    internal class Cast<T> : ExpressionVisitor
    {
        private readonly ParameterExpression _parameter;

        private Cast(ParameterExpression parameter)
        {
            _parameter = parameter;
        }

        public static Expression Create(Expression expression, ParameterExpression parameter)
        {
            return new Cast<T>(parameter).Visit(expression);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return Expression.Convert(_parameter, typeof(T));
        }
    }
}