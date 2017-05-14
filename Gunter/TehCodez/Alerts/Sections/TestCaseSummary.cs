using Gunter.Data;
using Gunter.Data.Sections;
using Gunter.Data.SqlClient;
using Gunter.Extensions;
using Gunter.Services;
using Reusable.Data;
using Reusable.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Gunter.Alerts.Sections
{
    public class TestCaseSummary : SectionFactory
    {
        public TestCaseSummary(ILogger logger) : base(logger) { Heading = "Test case"; }

        protected override ISection CreateCore(TestContext context)
        {
            var body =
                new DataTable(Heading)
                .AddColumn("Property", c => c.DataType = typeof(string))
                .AddColumn("Value", c => c.DataType = typeof(string))
                .AddRow($"{nameof(TestCase.Severity)}", context.Test.Severity.ToString())
                .AddRow($"{nameof(TestCase.Filter)}", context.Test.Filter)
                .AddRow($"{nameof(TestCase.Expression)}", context.Test.Expression)
                .AddRow($"{nameof(TestCase.Assert)}", context.Test.Assert)
                .AddRow($"{nameof(TestCase.BreakOnFailure)}", context.Test.BreakOnFailure)
                .AddRow($"{nameof(TestCase.Profiles)}", $"[{string.Join(", ", context.Test.Profiles.Select(p => $"'{p}'"))}]");

            
            return new TableSection
            {
                Heading = Heading,
                Body = body,
                Orientation = Orientation.Vertical
            };
        }
    }

}
