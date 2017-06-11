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
    internal class FakeDataSource : IDataSource, IEnumerable<DataRow>
    {
        private readonly DataTable _data;

        public FakeDataSource(int id)
        {
            Id = id;
            _data = new DataTable("Log");
            _data.Columns.Add("Id", typeof(int));
            _data.Columns.Add(nameof(FakeDataSourceExtensions.Timestamp), typeof(DateTime));
            _data.Columns.Add(nameof(FakeDataSourceExtensions.Environment), typeof(string));
            _data.Columns.Add(nameof(FakeDataSourceExtensions.LogLevel), typeof(string));
            _data.Columns.Add(nameof(FakeDataSourceExtensions.ElapsedSeconds), typeof(float));
            _data.Columns.Add(nameof(FakeDataSourceExtensions.Message), typeof(string));
            _data.Columns.Add(nameof(FakeDataSourceExtensions.Exception), typeof(string));
            _data.Columns.Add(nameof(FakeDataSourceExtensions.ItemCount), typeof(int));
            _data.Columns.Add(nameof(FakeDataSourceExtensions.Completed), typeof(bool));
        }

        public IVariableResolver Variables { get; set; } = VariableResolver.Empty;

        public int Id { get; set; }

        public DataTable Data => _data;

        public IEnumerable<(string Name, string Text)> GetCommands()
        {
            yield break;
        }

        public IEnumerator<DataRow> GetEnumerator()
        {
            return _data.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            _data.Dispose();
        }
    }

    internal static class FakeDataSourceExtensions
    {
        public static FakeDataSource AddRow(this FakeDataSource dataSource, Action<DataRow> row)
        {
            var newRow = dataSource.Data.NewRow();
            row(newRow);
            dataSource.Data.Rows.Add(newRow);
            return dataSource;
        }

        public static DataRow Timestamp(this DataRow row, DateTime timestamp)
        {
            row[nameof(Timestamp)] = timestamp;
            return row;
        }

        public static DataRow Environment(this DataRow row, string environment)
        {
            row[nameof(Environment)] = environment;
            return row;
        }

        public static DataRow LogLevel(this DataRow row, string logLevel)
        {
            row[nameof(logLevel)] = logLevel;
            return row;
        }

        public static DataRow ElapsedSeconds(this DataRow row, float elapsedSeconds)
        {
            row[nameof(ElapsedSeconds)] = elapsedSeconds;
            return row;
        }

        public static DataRow Message(this DataRow row, string message)
        {
            row[nameof(Message)] = message;
            return row;
        }

        public static DataRow Exception(this DataRow row, string exception)
        {
            row[nameof(Exception)] = exception;
            return row;
        }

        public static DataRow ItemCount(this DataRow row, int itemCount)
        {
            row[nameof(ItemCount)] = itemCount;
            return row;
        }

        public static DataRow Completed(this DataRow row, bool completed)
        {
            row[nameof(Completed)] = completed;
            return row;
        }
    }
}
