using Gunter.Data.Configuration.Abstractions;
using Gunter.Data.Configuration.Reporting;
using Gunter.Data.Configuration.Sections;
using Gunter.Services.Abstractions;
using Gunter.Workflow.Data;
using Reusable.Extensions;
using Reusable.Utilities.Mailr.Models;

namespace Gunter.Services.Reporting
{
    public class RenderTestSummary : IRenderReportModule
    {
        public RenderTestSummary(ITryGetFormatValue tryGetFormatValue, TestContext testContext)
        {
            TryGetFormatValue = tryGetFormatValue;
            TestContext = testContext;
        }

        private ITryGetFormatValue TryGetFormatValue { get; }

        private TestContext TestContext { get; }

        public IReportModuleDto Execute(ReportModule model) => Execute(model as TestSummary);

        private IReportModuleDto Execute(TestSummary model)
        {
            var table = new HtmlTable
            (
                ("Property", typeof(string)),
                ("Value", typeof(string))
            );

            table.Body.AddRow()
                .Set(Columns.Property, nameof(TestCase.Filter))
                .Set(Columns.Value, TestContext.TestCase.Filter);
            table.Body.AddRow()
                .Set(Columns.Property, nameof(TestCase.Assert))
                .Set(Columns.Value, TestContext.TestCase.Assert);
            table.Body.AddRow()
                .Set(Columns.Property, "When")
                .Set(Columns.Value, TestContext.Result.ToString(), TestContext.Result.ToString().ToLower());
            table.Body.AddRow()
                .Set(Columns.Property, "Then")
                .Set(Columns.Value, TestContext.TestCase.When[TestContext.Result]);
            table.Body.AddRow()
                .Set(Columns.Property, nameof(TestCase.Tags))
                .Set(Columns.Value, TestContext.TestCase.Tags);
            table.Body.AddRow()
                .Set(Columns.Property, "Elapsed")
                .Set(Columns.Value, $"{{{nameof(TestContext)}.{nameof(TestContext.EvaluateDataElapsed)}:{model.TimespanFormat}}}".Format(TryGetFormatValue));

            return new ReportModuleDto<TestSummary>(model, testInfo => new
            {
                Data = table
            });
        }

        private static class Columns
        {
            public const string Property = nameof(Property);

            public const string Value = nameof(Value);
        }
    }
}