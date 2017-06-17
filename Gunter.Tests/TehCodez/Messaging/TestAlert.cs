using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Gunter.Data;
using Gunter.Messaging;
using Gunter.Reporting;
using Gunter.Services;
using Reusable.Logging;

namespace Gunter.Tests.Messaging
{
    internal class TestAlert : Alert, IDisposable
    {
        public TestAlert() : base(new NullLogger()) { }

        public List<TestReport> PublishedReports { get; } = new List<TestReport>();

        protected override void PublishCore(TestUnit testUnit, IReport report)
        {
            var sections =
                from s in report.Modules
                select new TestSection
                {
                    Heading = s.Heading,
                    Text = s.Text,
                    //Detail = s.Detail?.Create(testUnit)
                };

            PublishedReports.Add(new TestReport
            {
                Title = report.Title,
                Sections = sections.ToList()
            });
        }

        public void Dispose()
        {
            foreach (var report in PublishedReports)
            {
                report.Dispose();
            }
        }
    }

    internal class TestReport : IDisposable
    {
        public string Title { get; set; }

        public List<TestSection> Sections { get; set; } = new List<TestSection>();

        public void Dispose()
        {
            foreach (var section in Sections)
            {
                section.Dispose();
            }
        }
    }

    internal class TestSection : IDisposable
    {
        public string Heading { get; set; }

        public string Text { get; set; }

        public DataSet Detail { get; set; }

        public void Dispose()
        {
            Detail?.Dispose();
        }
    }
}
