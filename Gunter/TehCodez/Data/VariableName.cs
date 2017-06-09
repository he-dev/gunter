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

        public static class Column
        {
            public static readonly string Timestamp = $"{nameof(Column)}.{nameof(Timestamp)}";
        }

        public class TestCollection
        {
            [Reserved]
            public static readonly string FileName = $"{nameof(TestCollection)}.{nameof(FileName)}";
        }

        public class TestCase
        {
            [Reserved]
            public static readonly string Severity = $"{nameof(TestCase)}.{nameof(Severity)}";

            [Reserved]
            public static readonly string Message = $"{nameof(TestCase)}.{nameof(Message)}";

            [Reserved]
            public static readonly string Profile = $"{nameof(TestCase)}.{nameof(Profile)}";
        }

        public static class DataSourceInfo
        {
            public static readonly string TimeSpanFormat = $"{nameof(DataSourceInfo)}.{nameof(TimeSpanFormat)}";
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

        public static readonly ImmutableDictionary<string, object> Default = new Dictionary<string, object>
        {
            [Column.Timestamp] = "Timestamp",

            // Custom TimeSpan Format Strings https://msdn.microsoft.com/en-us/library/ee372287(v=vs.110).aspx
            [DataSourceInfo.TimeSpanFormat] = @"dd\.hh\:mm\:ss"
        }
        .ToImmutableDictionary();


    }

    internal class ReservedNameException : Exception
    {
        public ReservedNameException(IEnumerable<string> names)
            : base(message: $"Reserved names found: [{string.Join(", ", names.Select(name => $"'{name}'"))}]")
        { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal class ReservedAttribute : Attribute { }
}
