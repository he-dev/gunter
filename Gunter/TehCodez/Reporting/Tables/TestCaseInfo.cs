using System.Data;
using System.Linq;
using Gunter.Data;
using Reusable.Data;

namespace Gunter.Reporting.Tables
{
    public class TestCaseInfo : ISectionDetail
    {
        public TableOrientation Orientation => TableOrientation.Vertical;

        public DataSet CreateDetail(TestContext context)
        {
            var body =
                new DataTable(nameof(TestCaseInfo))
                    .AddColumn("Property", c => c.DataType = typeof(string))
                    .AddColumn("Value", c => c.DataType = typeof(string))
                    .AddRow($"{nameof(TestCase.Severity)}", context.Test.Severity.ToString())
                    .AddRow($"{nameof(TestCase.Filter)}", context.Test.Filter)
                    .AddRow($"{nameof(TestCase.Expression)}", context.Test.Expression)
                    .AddRow($"{nameof(TestCase.Assert)}", context.Test.Assert)
                    .AddRow($"{nameof(TestCase.ContinueOnFailure)}", context.Test.ContinueOnFailure)
                    .AddRow($"{nameof(TestCase.Profiles)}", $"[{string.Join(", ", context.Test.Profiles.Select(p => $"'{p}'"))}]");

            return new DataSet { Tables = { body } };
        }
    }

}
