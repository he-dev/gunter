using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using Gunter.Data;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable.Data;
using Reusable.Extensions;
using Reusable.IOnymous.Http.Mailr.Models;

namespace Gunter.Reporting.Modules
{
    public class TestCase : Module, ITabular
    {
        public TableOrientation Orientation => TableOrientation.Vertical;

        public bool HasFoot => false;

        //[DefaultValue(@"dd\.hh\:mm\:ss")]
        [DefaultValue(@"mm\:ss\.fff")]
        public string TimespanFormat { get; set; }

        public override IModuleDto CreateDto(TestContext context)
        {
            var section = new ModuleDto<TestCase>
            {
                Heading = Heading.Format(context.RuntimeVariables),
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
                .Update(Columns.Property, "Elapsed")
                .Update(Columns.Value, $"{RuntimeVariables.TestCounter.AssertElapsed.ToString(TimespanFormat)}".Format(context.RuntimeVariables));
            section.Data.Body.NewRow()
                .Update(Columns.Property, nameof(Gunter.Data.TestCase.Tags))
                .Update(Columns.Value, context.TestCase.Tags);
            //.Update(Columns.Value, $"[{string.Join(", ", context.TestCase.Tags.Select(p => $"'{p}'"))}]");

            return section;
        }

        private static class Columns
        {
            public const string Property = nameof(Property);

            public const string Value = nameof(Value);
        }
    }
}