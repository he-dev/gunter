using System.Collections.Generic;
using System.Linq;
using Gunter.Data;
using Gunter.Messaging;
using Gunter.Reporting;
using Gunter.Services;
using Reusable.Logging;

namespace Gunter.Tests.Messaging
{
    internal class MockAlert : Alert
    {
        public MockAlert() : base(new NullLogger()) { }

        public List<(TestContext Context, IReport Report)> Contexts { get; } = new List<(TestContext Context, IReport Report)>();

        protected override void PublishCore(TestContext context, IReport report)
        {
            Contexts.Add((context, report));
        }
    }
}
