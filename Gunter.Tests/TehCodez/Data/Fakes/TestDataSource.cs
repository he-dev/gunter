using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Gunter.Data;
using Gunter.Services;
using Reusable.Data;

namespace Gunter.Tests.Data.Fakes
{
    internal class TestDataSource : IDataSource, IEnumerable<DataRow>
    {
        public TestDataSource(int id)
        {
            Id = id;
            Data = new DataTable("TestData");            
            Data.Columns.Add("_id", typeof(int));
            Data.Columns.Add(nameof(TestDataSourceExtensions._string), typeof(string));
            Data.Columns.Add(nameof(TestDataSourceExtensions._int), typeof(int));
            Data.Columns.Add(nameof(TestDataSourceExtensions._double), typeof(double));
            Data.Columns.Add(nameof(TestDataSourceExtensions._decimal), typeof(decimal));
            Data.Columns.Add(nameof(TestDataSourceExtensions._datetime), typeof(DateTime));
            Data.Columns.Add(nameof(TestDataSourceExtensions._bool), typeof(bool));
        }

        public IVariableResolver Variables { get; set; } = VariableResolver.Empty;

        public int Id { get; set; }

        public DataTable Data { get; }

        public TimeSpan Elapsed { get; }

        public IEnumerable<(string Name, string Text)> GetCommands()
        {
            yield break;
        }

        public IEnumerator<DataRow> GetEnumerator()
        {
            return Data.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            Data.Dispose();
        }
    }

    internal static class TestDataSourceExtensions
    {
        public static TestDataSource AddRow(this TestDataSource dataSource, Action<DataRow> row)
        {
            var newRow = dataSource.Data.NewRow();
            row(newRow);
            dataSource.Data.Rows.Add(newRow);
            return dataSource;
        }

        public static DataRow _string(this DataRow row, object _string)
        {
            row[nameof(TestDataSourceExtensions._string)] = _string ?? DBNull.Value;
            return row;
        }

        public static DataRow _int(this DataRow row, object _int)
        {
            row[nameof(TestDataSourceExtensions._int)] = _int ?? DBNull.Value;
            return row;
        }

        public static DataRow _double(this DataRow row, object _double)
        {
            row[nameof(_double)] = _double ?? DBNull.Value;
            return row;
        }

        public static DataRow _decimal(this DataRow row, object _decimal)
        {
            row[nameof(TestDataSourceExtensions._decimal)] = _decimal ?? DBNull.Value;
            return row;
        }

        public static DataRow _datetime(this DataRow row, object _datetime)
        {
            row[nameof(TestDataSourceExtensions._datetime)] = _datetime ?? DBNull.Value;
            return row;
        }

        public static DataRow _bool(this DataRow row, object _bool)
        {
            row[nameof(TestDataSourceExtensions._bool)] = _bool ?? DBNull.Value;
            return row;
        }
    }
}
