using System.Collections.Generic;
using System;
using Gunter.Services;
using System.Data;
using System.Linq;
using Gunter.Messaging;
using Gunter.Reporting;

namespace Gunter.Data
{
    public class TestConfiguration
    {
        public TestCase Test { get; set; }

        public IEnumerable<IDataSource> DataSources { get; set; }

        public IEnumerable<IAlert> Alerts { get; set; }

        public IEnumerable<IReport> Reports { get; set; }

        public IConstantResolver Constants { get; set; }
    }

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
