using System.ComponentModel;
using Gunter.Data.Configuration;
using Gunter.Reporting;
using Gunter.Workflows;
using Reusable.Utilities.Mailr.Models;

namespace Gunter.Data.Reporting
{
    public class TestInfo : RenderDto<>, ITabular
    {
        public TableOrientation Orientation => TableOrientation.Vertical;

        public bool HasFoot => false;

        //[DefaultValue(@"dd\.hh\:mm\:ss")]
        [DefaultValue(@"mm\:ss\.fff")]
        public string TimespanFormat { get; set; }

        public override IReportModule Create(TestContext context)
        {
            var section = new ReportModule<TestInfo>
            {
                Heading = Heading.Format(context.Container),
                Data = new HtmlTable(HtmlTableColumn.Create
                (
                    ("Property", typeof(string)),
                    ("Value", typeof(string))
                ))
            };

            section.Data.Body.NewRow()
                .Update(Columns.Property, nameof(TestCase.Filter))
                .Update(Columns.Value, context.TestCase.Filter);
            section.Data.Body.NewRow()
                .Update(Columns.Property, nameof(TestCase.Assert))
                .Update(Columns.Value, context.TestCase.Assert);
            section.Data.Body.NewRow()
                .Update(Columns.Property, "When")
                .Update(Columns.Value, context.Result.ToString(), context.Result.ToString().ToLower());
            section.Data.Body.NewRow()
                .Update(Columns.Property, "Then")
                .Update(Columns.Value, context.TestCase.Messages[context.Result]);
            section.Data.Body.NewRow()
                .Update(Columns.Property, nameof(TestCase.Tags))
                .Update(Columns.Value, context.TestCase.Tags);
            section.Data.Body.NewRow()
                .Update(Columns.Property, "Elapsed")
                .Update(Columns.Value, $"{RuntimeProperty.BuiltIn.TestContext.EvaluateDataElapsed.ToFormatString(TimespanFormat)}".Format(context.Container));

            return section;
        }

        private static class Columns
        {
            public const string Property = nameof(Property);

            public const string Value = nameof(Value);
        }
    }
}