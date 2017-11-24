using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using Gunter.Data;
using JetBrains.Annotations;
using Reusable.Data;

namespace Gunter.Reporting.Modules
{
    public class TestCaseInfo : Module, ITabular
    {
        public TableOrientation Orientation => TableOrientation.Vertical;

        public bool HasFooter => false;

        //[DefaultValue(@"dd\.hh\:mm\:ss")]
        [DefaultValue(@"mm\:ss\.fff")]
        public string TimespanFormat { get; set; }

        public DataTable Create(TestContext context)
        {
            var table =
                new DataTable(nameof(TestCaseInfo))
                    .AddColumn("Property", c => c.DataType = typeof(string))
                    .AddColumn("Value", c => c.DataType = typeof(string))
                    //.AddRow(nameof(TestCase.Severity), testUnit.Test.Severity.ToString())
                    .AddRow(nameof(TestCase.Filter), context.TestCase.Filter)
                    .AddRow(nameof(TestCase.Expression), context.TestCase.Expression)
                    .AddRow(nameof(TestCase.Assert), context.TestCase.Assert)
                    .AddRow(nameof(TestCase.OnPassed), context.TestCase.OnPassed)
                    .AddRow(nameof(TestCase.OnFailed), context.TestCase.OnFailed)
                    .AddRow(nameof(TestContext.GetDataElapsed), context.GetDataElapsed.ToString(TimespanFormat, CultureInfo.InvariantCulture)) // @"hh\:mm\:ss\.f")) /
                    .AddRow(nameof(TestCase.Profiles), $"[{string.Join(", ", context.TestCase.Profiles.Select(p => $"'{p}'"))}]");

            return table;
        }
    }
}
