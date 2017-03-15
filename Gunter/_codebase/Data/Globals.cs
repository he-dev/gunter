using Gunter.Services;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gunter.Data
{
    internal static class Globals
    {
        public static class Columns
        {
            //public static readonly string PrimaryKey = $"{nameof(Columns)}.{nameof(PrimaryKey)}";
            public static readonly string Timestamp = $"{nameof(Columns)}.{nameof(Timestamp)}";
            //public static readonly string Exception = $"{nameof(Columns)}.{nameof(Exception)}";
            //public static readonly string Message = $"{nameof(Columns)}.{nameof(Message)}";
        }

        internal class Test
        {
            public static readonly string FileName = $"{nameof(Test)}.{nameof(FileName)}";
            public static readonly string Severity = $"{nameof(Test)}.{nameof(Severity)}";
        }

        internal static class DataSourceInfo
        {
            public static readonly string TimeSpanFormat = $"{nameof(DataSourceInfo)}.{nameof(TimeSpanFormat)}";
        }

        public static readonly string Environment = nameof(Environment);

        public static readonly ImmutableList<string> ReservedNames = new[]
        {
            Test.FileName,
            Test.Severity,
            Environment            
        }
        .ToImmutableList();

        public static readonly ImmutableDictionary<string, object> Default = new Dictionary<string, object>
        {
            //[Columns.PrimaryKey] = "Id",
            [Columns.Timestamp] = "Timestamp",
            //[Columns.Exception] = "Exception"
            
            // Custom TimeSpan Format Strings https://msdn.microsoft.com/en-us/library/ee372287(v=vs.110).aspx
            [DataSourceInfo.TimeSpanFormat] = @"dd\.hh\:mm\:ss"
        }
        .ToImmutableDictionary();

        public static void ValidateNames(IConstantResolver globals)
        {
            var duplicates = ReservedNames.Where(reservedName => globals.ContainsKey(reservedName)).ToList();
            if (duplicates.Any()) throw new Exception();
        }
    }

    internal class ReservedNameException : Exception
    {
        public ReservedNameException(IEnumerable<string> names)
            : base(message: $"Reserved names found: [{string.Join(", ", names.Select(name => $"'{name}'"))}]")
        { }
    }
}
