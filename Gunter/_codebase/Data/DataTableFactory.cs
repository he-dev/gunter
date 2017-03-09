using System.Data;

namespace Gunter.Data
{
    internal static class DataTableFactory
    {
        public static DataTable Create(string name, string[] columnNames)
        {
            var dataTable = new DataTable(name);
            foreach (var columnName in columnNames) dataTable.Columns.Add(columnName, typeof(string));
            return dataTable;
        }
    }
}
