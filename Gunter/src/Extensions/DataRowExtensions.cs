using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Gunter.Reporting;

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
        
        /// <summary>
        /// Creates a group-key for the specified row.
        /// </summary>
        public static IEnumerable<object> GroupKey(this DataRow dataRow, IEnumerable<DataInfoColumn> keyColumns)
        {
            // Get key values and apply their filters.
            //return keyColumns.Select(column => column.Filter.Apply(dataRow[column.Name.ToString()]));
            return keyColumns.Select(column => dataRow[column.Select.ToString()]);
        }
    }
}