using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using Gunter.Data;
using JetBrains.Annotations;
using Reusable.Data;

namespace Gunter.Reporting.Modules
{
    public class TestCase : Module, ITabular
    {
        public TableOrientation Orientation => TableOrientation.Vertical;

        public bool HasFoot => false;

        //[DefaultValue(@"dd\.hh\:mm\:ss")]
        [DefaultValue(@"mm\:ss\.fff")]
        public string TimespanFormat { get; set; }

        public DataTable Create(TestContext context)
        {
            var format = (FormatFunc) context.Formatter.Format;

            var table =
                new DataTable(nameof(TestCase))
                    .AddColumn("Property", c => c.DataType = typeof(string))
                    .AddColumn("Value", c => c.DataType = typeof(string))
                    //.AddRow(nameof(TestCase.Severity), testUnit.Test.Severity.ToString())
                    .AddRow(nameof(Gunter.Data.TestCase.Filter), context.TestCase.Filter)
                    .AddRow(nameof(Gunter.Data.TestCase.Expression), context.TestCase.Expression)
                    .AddRow(nameof(Gunter.Data.TestCase.Assert), context.TestCase.Assert)
                    .AddRow(nameof(Gunter.Data.TestCase.OnPassed), context.TestCase.OnPassed)
                    .AddRow(nameof(Gunter.Data.TestCase.OnFailed), context.TestCase.OnFailed)
                    .AddRow(nameof(Gunter.Data.TestStatistic.AssertElapsed), format($"{{{RuntimeVariableHelper.TestStatistic.AssertElapsed.Name.ToString()}:{TimespanFormat}}}")) // @"hh\:mm\:ss\.f")) /
                    .AddRow(nameof(Gunter.Data.TestCase.Profiles), $"[{string.Join(", ", context.TestCase.Profiles.Select(p => $"'{p}'"))}]");

            return table;
        }
    }
}
