using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using Gunter.Data;
using Gunter.Data.Dtos;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable.Data;
using Reusable.IOnymous.Models;

namespace Gunter.Reporting.Modules
{
    public class TestCase : Module, ITabular
    {
        public TableOrientation Orientation => TableOrientation.Vertical;

        public bool HasFoot => false;

        //[DefaultValue(@"dd\.hh\:mm\:ss")]
        [DefaultValue(@"mm\:ss\.fff")]
        public string TimespanFormat { get; set; }

        public override ModuleDto CreateDto(TestContext context)
        {
            var format = (FormatFunc)context.Formatter.Format;

            var section = new ModuleDto
            {
                Heading = format(Heading),
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
                   .Update(Columns.Property, nameof(Gunter.Data.TestContext.Result))
                   .Update(Columns.Value, context.Result.ToString(), context.Result.ToString().ToLower());
            section.Data.Body.NewRow()
                   .Update(Columns.Property, nameof(Gunter.Data.TestCase.OnPassed))
                   .Update(Columns.Value, context.TestCase.OnPassed.ToString());
            section.Data.Body.NewRow()
                   .Update(Columns.Property, nameof(Gunter.Data.TestCase.OnFailed))
                   .Update(Columns.Value, context.TestCase.OnFailed.ToString());
            section.Data.Body.NewRow()
                   .Update(Columns.Property, nameof(Gunter.Data.TestCounter.RunTestElapsed))
                   .Update(Columns.Value, format($"{RuntimeVariable.TestCounter.AssertElapsed.ToString(TimespanFormat)}"));
            section.Data.Body.NewRow()
                   .Update(Columns.Property, nameof(Gunter.Data.TestCase.Profiles))
                   .Update(Columns.Value, $"[{string.Join(", ", context.TestCase.Profiles.Select(p => $"'{p}'"))}]");

            return section;
        }

        private static class Columns
        {
            public const string Property = nameof(Property);

            public const string Value = nameof(Value);
        }
    }
}