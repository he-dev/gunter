using System.Data;
using System.Linq;
using Gunter.Data;
using Reusable.Data;

namespace Gunter.Reporting.Modules
{
    public class TestCaseInfo : Module, ITabular
    {
        public TableOrientation Orientation => TableOrientation.Vertical;

        public bool HasFooter => false;

        public DataTable Create(TestUnit testUnit)
        {
            var table =
                new DataTable(nameof(TestCaseInfo))
                    .AddColumn("Property", c => c.DataType = typeof(string))
                    .AddColumn("Value", c => c.DataType = typeof(string))
                    //.AddRow(nameof(TestCase.Severity), testUnit.Test.Severity.ToString())
                    .AddRow(nameof(TestCase.Filter), testUnit.TestCase.Filter)
                    .AddRow(nameof(TestCase.Expression), testUnit.TestCase.Expression)
                    .AddRow(nameof(TestCase.Assert), testUnit.TestCase.Assert)
                    .AddRow(nameof(TestCase.OnPassed), testUnit.TestCase.OnPassed)
                    .AddRow(nameof(TestCase.OnFailed), testUnit.TestCase.OnFailed)
                    .AddRow(nameof(TestCase.Elapsed), testUnit.TestCase.Elapsed.ToString(@"hh\:mm\:ss\.f")) // todo hardcoded timespan format
                    .AddRow(nameof(TestCase.Profiles), $"[{string.Join(", ", testUnit.TestCase.Profiles.Select(p => $"'{p}'"))}]");

            return table;
        }
    }
}
