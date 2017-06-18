using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace Gunter.Services
{
    internal interface IVariableBuilder
    {
        IEnumerable<string> Names { get; }
        VariableBuilder AddVariables<T>([NotNull] params Expression<Func<T, object>>[] expressions);
        IEnumerable<KeyValuePair<string, object>> BuildVariables<T>(T obj);
    }

    internal class VariableBuilder : IVariableBuilder
    {
        private readonly IDictionary<Type, HashSet<INameable>> _variables = new Dictionary<Type, HashSet<INameable>>();

        public IEnumerable<string> Names => _variables.Values.SelectMany(x => x).Select(x => x.Name);

        public VariableBuilder AddVariables<T>(params Expression<Func<T, object>>[] expressions)
        {
            if (expressions == null) { throw new ArgumentNullException(nameof(expressions)); }

            // We could use reflection here but since there are only a couple of variables and this is not performance relevat
            // let's use expressions for educational purposes.

            foreach (var expression in expressions)
            {
                var variable = Variable<T>.Create(
                    name: CreateVarialbeName(expression), 
                    getValue: expression.Compile());

                if (_variables.TryGetValue(typeof(T), out var variables))
                {
                    if (!variables.Add(variable))
                    {
                        throw new ArgumentException($"Variable \"{variable.Name}\" has already been added.");
                    }
                }
                else
                {
                    _variables.Add(typeof(T), new HashSet<INameable> { variable });
                }
            }

            return this;
        }

        private static string CreateVarialbeName(Expression expression)
        {
            while (true)
            {
                expression = expression is LambdaExpression lambda ? lambda.Body : expression;

                if (expression is MemberExpression memberExpression)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    // For member expression the DeclaringType cannot be null.
                    return $"{memberExpression.Member.DeclaringType.Name}.{memberExpression.Member.Name}";
                }

                if (expression is UnaryExpression unaryExpression)
                {
                    expression = unaryExpression.Operand;
                    continue;
                }

                throw new ArgumentException("Member expression not found.");
            }
        }

        public IEnumerable<KeyValuePair<string, object>> BuildVariables<T>(T obj)
        {
            if (_variables.TryGetValue(typeof(T), out var variables))
            {
                foreach (var variable in variables.Cast<Variable<T>>())
                {
                    yield return new KeyValuePair<string, object>(variable.Name, variable.GetValue(obj));
                }
            }
        }
    }

    internal interface INameable : IEquatable<INameable>
    {
        string Name { get; }
    }

    internal class Variable<T> : INameable
    {
        private Variable([NotNull] string name, [NotNull] Func<T, object> getValue)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            GetValue = getValue ?? throw new ArgumentNullException(nameof(getValue));
        }

        [NotNull]
        public string Name { get; }

        [NotNull]
        public Func<T, object> GetValue { get; }

        [NotNull]
        public static INameable Create(string name, Func<T, object> getValue)
        {
            return new Variable<T>(name, getValue);
        }

        public bool Equals(INameable nameable)
        {
            return Name.Equals(nameable?.Name);
        }

        public override int GetHashCode() => Name.GetHashCode();
    }
}