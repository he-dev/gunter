using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Gunter.Messaging;
using Gunter.Reporting;
using Gunter.Services;
using JetBrains.Annotations;

namespace Gunter.Data
{
    public class TestUnit
    {
        public TestFile TestFile { get; set; }

        public TestCase TestCase { get; set; }

        public int TestNumber { get; set; }

        public IDataSource DataSource { get; set; }

        public IEnumerable<IAlert> Alerts { get; set; }

        public IEnumerable<IReport> Reports { get; set; }       
    }
}