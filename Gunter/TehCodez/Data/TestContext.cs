using System.Collections.Generic;
using System;
using Gunter.Services;
using System.Data;

namespace Gunter.Data
{
    public class TestContext : IDisposable
    {
        private DataTable _data;

        public TestCase Test { get; set; }

        public IDataSource DataSource { get; set; }

        public IEnumerable<IAlert> Alerts { get; set; }

        public IConstantResolver Constants { get; set; }

        public DataTable Data => _data ?? (_data = DataSource.GetData(Constants));

        public void Dispose()
        {
            _data?.Dispose();
        }
    }
}
