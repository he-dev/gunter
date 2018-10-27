using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Gunter.Extensions
{
    internal static class DataRowExtensions
    {
        public static IEnumerable<object> Values(this IEnumerable<DataRow> dataRows, string columnName)
        {
            return dataRows.Select(dataRow => dataRow[columnName]);
        }

        // ReSharper disable once InconsistentNaming
        public static IEnumerable<object> NotDBNull(this IEnumerable<object> dataRows)
        {
            return dataRows.Where(value => value != DBNull.Value);
        }        
    }
}