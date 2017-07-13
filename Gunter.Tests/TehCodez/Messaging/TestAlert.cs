using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Gunter.Data;
using Gunter.Messaging;
using Gunter.Reporting;
using Gunter.Services;
using Reusable.Logging.Loggex;

namespace Gunter.Tests.Messaging
{
    internal class TestAlert : Alert, IDisposable
    {
        private readonly IDictionary<string, List<TestReport>> PublishedReports = new Dictionary<string, List<TestReport>>(StringComparer.OrdinalIgnoreCase);

        public TestAlert() : base(Reusable.Logging.Loggex.Logger.Create<TestAlert>()) { }

        public List<TestReport> GetReports(string testName)
        {
            //var testName = Regex.Split(callerName, "_")[1];
            return PublishedReports.TryGetValue($"{testName}.json", out var reports) ? reports : new List<TestReport>();
        }

        protected override void PublishCore(TestUnit testUnit, IReport report)
        {
            var sections =
                from module in report.Modules
                select new TestSection
                {
                    Heading = module.Heading,
                    Text = module.Text,
                    //Detail = s.Detail?.Create(testUnit)
                };

            var testName = Path.GetFileName(testUnit.FullName);
            var testReport = new TestReport
            {
                Title = report.Title,
                Sections = sections.ToList()
            };

            if (PublishedReports.TryGetValue(testName, out var reports))
            {
                reports.Add(testReport);
            }
            else
            {
                PublishedReports.Add(testName, new List<TestReport> { testReport });
            }
        }

        public void Dispose()
        {
            foreach (var report in PublishedReports)
            {
                //report.Dispose();
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
