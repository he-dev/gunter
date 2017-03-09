using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gunter.Data
{
    internal static class Globals
    {
        public static string FileName = $"{nameof(Columns)}.{nameof(FileName)}";

        public static class Columns
        {
            public static string PrimaryKey = $"{{{nameof(Columns)}.{nameof(PrimaryKey)}}}";
            public static string Timestamp = $"{{{nameof(Columns)}.{nameof(Timestamp)}}}";
            public static string Exception = $"{{{nameof(Columns)}.{nameof(Exception)}}}";
            public static string Message = $"{{{nameof(Columns)}.{nameof(Message)}}}";
        }
    }
}
