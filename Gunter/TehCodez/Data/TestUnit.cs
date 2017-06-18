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
    public class TestUnit : IDisposable
    {
        [NotNull]
        public string FullName { get; set; }

        public string FileName => Path.GetFileName(FullName);

        public TestCase TestCase { get; set; }

        public int TestNumber { get; set; }

        public IDataSource DataSource { get; set; }

        public IEnumerable<IAlert> Alerts { get; set; }

        public IEnumerable<IReport> Reports { get; set; }

        public void Dispose()
        {
            DataSource?.Dispose();
        }
    }
}