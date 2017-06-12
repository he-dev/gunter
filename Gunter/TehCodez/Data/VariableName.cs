using Gunter.Services;
using Reusable;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Reusable.Extensions;

namespace Gunter.Data
{
    internal static class VariableName
    {
        [Reserved]
        public static readonly string Environment = nameof(Environment);       

        public static class TestFile
        {
            [Reserved]
            public static readonly string FileName = $"{nameof(TestFile)}.{nameof(FileName)}";

            public static string Name { get; private set; }
        }

        public static class TestCase
        {
            [Reserved]
            public static readonly string Severity = $"{nameof(TestCase)}.{nameof(Severity)}";

            [Reserved]
            public static readonly string Message = $"{nameof(TestCase)}.{nameof(Message)}";

            //[Reserved]
            //public static readonly string Profile = $"{nameof(TestCase)}.{nameof(Profile)}";
        }

        public static IEnumerable<string> GetReservedNames()
        {
            return
                typeof(VariableName)
                .Flatten()
                .Select(t => t.Type
                    .GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(f => f.GetCustomAttribute<ReservedAttribute>().IsNotNull()))
                .SelectMany(fields => fields)
                .Select(f => (string)f.GetValue(null));
        }
    }

    internal class ReservedVariableNameException : Exception
    {
        public ReservedVariableNameException(IEnumerable<string> names)
            : base($"You must not use any of these reserved names: [{string.Join(", ", names.Select(name => $"'{name}'"))}]")
        { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal class ReservedAttribute : Attribute { }
}
