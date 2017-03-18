using Gunter.Alerts;
using Gunter.Data;
using Gunter.Data.Sections;
using Gunter.Services;
using System;
using System.Collections.Generic;
using System.Data;
using Reusable.Logging;
using System.Linq;

namespace Gunter.Tests.Alerting
{
    internal class MockAlert : Alert
    {
        public MockAlert() : this(new NullLogger()) { }

        public MockAlert(ILogger logger) : base(logger) { }

        public List<List<ISection>> Data { get; set; } = new List<List<ISection>>();

        protected override void PublishCore(IEnumerable<ISection> sections, IConstantResolver constants)
        {
            Data.Add(sections.ToList());
        }
    }
}
