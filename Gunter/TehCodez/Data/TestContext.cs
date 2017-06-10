using System.Collections.Generic;
using System.Data;
using Gunter.Messaging;
using Gunter.Reporting;
using Gunter.Services;

namespace Gunter.Data
{
    public class TestContext
    {
        public TestContext(TestConfiguration configuration, IDataSource dataSource, DataTable data)
        {
            Test = configuration.Test;
            DataSource = dataSource;
            Data = data;
            Alerts = configuration.Alerts;
            Reports = configuration.Reports;
            Constants = configuration.Constants;
        }

        public TestCase Test { get; }

        public IDataSource DataSource { get; }

        public DataTable Data { get; }

        public IEnumerable<IAlert> Alerts { get; }

        public IEnumerable<IReport> Reports { get; }

        public IConstantResolver Constants { get; }
    }
}