using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Reusable.Data;
using Gunter.Data;
using Gunter.Services;

namespace Gunter.Tests.Data
{
    internal class MockDataSource : IDataSource, IEnumerable<DataRow>
    {
        private readonly DataTable _data;
        private readonly DateTime _timestamp;

        public MockDataSource(int id)
        {
            Id = id;
            _data = new DataTable("Log");
            _data.Columns.Add("Id", typeof(int));
            _data.Columns.Add("Timestamp", typeof(DateTime));
            _data.Columns.Add("Environment", typeof(string));
            _data.Columns.Add("LogLevel", typeof(string));
            _data.Columns.Add("ElapsedSeconds", typeof(float));
            _data.Columns.Add("Message", typeof(string));
            _data.Columns.Add("Exception", typeof(string));
            _timestamp = DateTime.Now;
        }

        public int Id { get; set; }

        public DataTable GetData(IConstantResolver constants)
        {
            return _data;
        }

        public void Add(string environment, string logLevel, float? elapsedSeconds, string message, string exception)
        {
            Add(_data.Rows.Count, _timestamp.AddMinutes(_data.Rows.Count), environment, logLevel, elapsedSeconds, message, exception);
        }

        private void Add(params object[] values)
        {
            _data.AddRow(values);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return base.ToString();
        }

        public IEnumerator<DataRow> GetEnumerator()
        {
            return _data.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
