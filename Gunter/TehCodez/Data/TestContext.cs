using System.Collections.Generic;
using System.Data;
using Gunter.Messaging;
using Gunter.Reporting;
using Gunter.Services;

namespace Gunter.Data
{
    public class TestContext
    {
        public TestCase Test { get; set; }

        public IDataSource DataSource { get; set; }

        public DataTable Data { get; set; }

        public IEnumerable<IAlert> Alerts { get; set; }

        public IEnumerable<IReport> Reports { get; set; }

        //public IConstantResolver Constants { get; set; }
    }
}