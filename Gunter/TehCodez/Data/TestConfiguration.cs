using System.Collections.Generic;
using System;
using Gunter.Services;
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
}
