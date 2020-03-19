using System.Collections.Generic;
using System.Data;
using System.Linq;
using Gunter.Data;
using Gunter.Data.Configuration.Reporting;

namespace Gunter.Extensions
{
    internal static class DataRowExtensions
    {
        /// <summary>
        /// Creates a group-key for the specified row.
        /// </summary>
        public static DataRowKey CreateKey(this DataRow dataRow, IEnumerable<DataColumnSetting> columns)
        {
            return new DataRowKey(columns.Select(column => dataRow[column.Select]));
        }
    }
}