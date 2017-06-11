using System;
using System.Collections.Generic;
using System.Data;
using Gunter.Messaging;
using Gunter.Reporting;
using Gunter.Services;

namespace Gunter.Data
{
    public class TestUnit : IDisposable
    {
        public TestCase Test { get; set; }

        public IDataSource DataSource { get; set; }

        public IEnumerable<IAlert> Alerts { get; set; }

        public IEnumerable<IReport> Reports { get; set; }

        public void Dispose()
        {
            DataSource?.Dispose();
        }
    }
}