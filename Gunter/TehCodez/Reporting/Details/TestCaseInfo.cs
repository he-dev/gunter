using System.Data;
using System.Linq;
using Gunter.Data;
using Reusable.Data;

namespace Gunter.Reporting.Details
{
    public class TestCaseInfo : ISectionDetail
    {
        public TableOrientation Orientation => TableOrientation.Vertical;

        public DataSet Create(TestUnit testUnit)
        {
            var body =
                new DataTable(nameof(TestCaseInfo))
                    .AddColumn("Property", c => c.DataType = typeof(string))
                    .AddColumn("Value", c => c.DataType = typeof(string))
                    .AddRow(nameof(TestCase.Severity), testUnit.Test.Severity.ToString())
                    .AddRow(nameof(TestCase.Filter), testUnit.Test.Filter)
                    .AddRow(nameof(TestCase.Expression), testUnit.Test.Expression)
                    .AddRow(nameof(TestCase.Assert), testUnit.Test.Assert)
                    .AddRow(nameof(TestCase.ContinueOnFailure), testUnit.Test.ContinueOnFailure)
                    .AddRow(nameof(TestCase.AlertTrigger), testUnit.Test.AlertTrigger)
                    .AddRow(nameof(TestCase.Profiles), $"[{string.Join(", ", testUnit.Test.Profiles.Select(p => $"'{p}'"))}]");

            return new DataSet { Tables = { body } };
        }
    }

}
