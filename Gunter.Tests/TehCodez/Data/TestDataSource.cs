using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Gunter.Data;
using Gunter.Services;
using Newtonsoft.Json;
using Reusable.Data;

namespace Gunter.Tests.Data
{
    [JsonObject]
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

            this
                .AddRow(row => row._id(1)._string("foo")._int(1)._double(2.2)._decimal(3.5m)._datetime(new DateTime(2017, 5, 1))._bool(true))
                .AddRow(row => row._id(2)._string("foo")._int(3)._double(null)._decimal(5m)._datetime(new DateTime(2017, 5, 2))._bool(true))
                .AddRow(row => row._id(3)._string("bar")._int(8)._double(null)._decimal(1m)._datetime(null)._bool(false))
                .AddRow(row => row._id(4)._string("bar")._int(null)._double(3.5)._decimal(null)._datetime(new DateTime(2017, 5, 3))._bool(false))
                .AddRow(row => row._id(5)._string("bar")._int(4)._double(7)._decimal(2m)._datetime(new DateTime(2017, 5, 4))._bool(null))
                .AddRow(row => row._id(6)._string(null)._int(null)._double(2.5)._decimal(8m)._datetime(new DateTime(2017, 5, 5))._bool(true))
                .AddRow(row => row._id(7)._string("")._int(2)._double(6)._decimal(1.5m)._datetime(new DateTime(2017, 5, 6))._bool(null));
        }

        public IVariableResolver Variables { get; set; } = VariableResolver.Empty;

        public int Id { get; set; }

        [JsonIgnore]
        public DataTable Data { get; }

        [JsonIgnore]
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

        public static DataRow _id(this DataRow row, object _id)
        {
            row[nameof(TestDataSourceExtensions._id)] = _id ?? DBNull.Value;
            return row;
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
