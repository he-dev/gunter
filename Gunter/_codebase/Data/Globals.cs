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
            public static string PrimaryKey = $"{nameof(Columns)}.{nameof(PrimaryKey)}";
            public static string Timestamp = $"{nameof(Columns)}.{nameof(Timestamp)}";
            public static string Exception = $"{nameof(Columns)}.{nameof(Exception)}";
            public static string Message = $"{nameof(Columns)}.{nameof(Message)}";
        }

        internal class Test
        {
            public static string FileName = $"{nameof(Test)}.{nameof(FileName)}";
            public static string Severity = $"{nameof(Test)}.{nameof(Severity)}";
        }

        public static ImmutableList<string> ReservedNames = new[]
        {
            Columns.PrimaryKey,
            Columns.Timestamp,
            Columns.Exception,
            Columns.Exception,
            Test.FileName,
            Test.Severity
        }
        .ToImmutableList();
    }
}
