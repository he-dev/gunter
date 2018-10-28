using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Gunter.Extensions
{
    internal static class DataRowExtensions
    {
        public static IEnumerable<object> Values(this IEnumerable<DataRow> rows, string columnName)
        {
            return rows.Select(row => row[columnName]);
        }

        // ReSharper disable once InconsistentNaming
        public static IEnumerable<object> NotDBNull(this IEnumerable<object> rows)
        {
            return rows.Where(value => value != DBNull.Value);
        }        
    }
}