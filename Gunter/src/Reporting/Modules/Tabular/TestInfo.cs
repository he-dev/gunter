using System.ComponentModel;
using Gunter.Data;
using Reusable.Extensions;
using Reusable.Utilities.Mailr.Models;

namespace Gunter.Reporting.Modules.Tabular
{
    public class TestInfo : Module, ITabular
    {
        public TableOrientation Orientation => TableOrientation.Vertical;

        public bool HasFoot => false;

        //[DefaultValue(@"dd\.hh\:mm\:ss")]
        [DefaultValue(@"mm\:ss\.fff")]
        public string TimespanFormat { get; set; }

        public override IModuleDto CreateDto(TestContext context)
        {
            var section = new ModuleDto<TestInfo>
            {
                Heading = Heading.Format(context.RuntimeProperties),
                Data = new HtmlTable(HtmlTableColumn.Create
                (
                    ("Property", typeof(string)),
                    ("Value", typeof(string))
                ))
            };

            section.Data.Body.NewRow()
                .Update(Columns.Property, nameof(Gunter.Data.TestCase.Filter))
                .Update(Columns.Value, context.TestCase.Filter);
            section.Data.Body.NewRow()
                .Update(Columns.Property, nameof(Gunter.Data.TestCase.Assert))
                .Update(Columns.Value, context.TestCase.Assert);
            section.Data.Body.NewRow()
                .Update(Columns.Property, "When")
                .Update(Columns.Value, context.Result.ToString(), context.Result.ToString().ToLower());
            section.Data.Body.NewRow()
                .Update(Columns.Property, "Then")
                .Update(Columns.Value, context.TestCase.When[context.Result]);
            section.Data.Body.NewRow()
                .Update(Columns.Property, nameof(Gunter.Data.TestCase.Tags))
                .Update(Columns.Value, context.TestCase.Tags);
            section.Data.Body.NewRow()
                .Update(Columns.Property, "Elapsed")
                .Update(Columns.Value, $"{RuntimeProperty.BuiltIn.TestCounter.AssertElapsed.ToFormatString(TimespanFormat)}".Format(context.RuntimeProperties));

            return section;
        }

        private static class Columns
        {
            public const string Property = nameof(Property);

            public const string Value = nameof(Value);
        }
    }
}