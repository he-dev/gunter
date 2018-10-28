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

namespace Gunter.Reporting.Modules
{
    public class TestCase : Module, ITabular
    {
        public TableOrientation Orientation => TableOrientation.Vertical;

        public bool HasFoot => false;

        //[DefaultValue(@"dd\.hh\:mm\:ss")]
        [DefaultValue(@"mm\:ss\.fff")]
        public string TimespanFormat { get; set; }

        public override SectionDto CreateDto(TestContext context)
        {
            var format = (FormatFunc)context.Formatter.Format;

            var section = new SectionDto
            {
                Table = new TripleTableDto(new[]
                {
                    ColumnDto.Create<string>("Property"),
                    ColumnDto.Create<string>("Value")
                })
            };
            var table = section.Table;

            table.Body.Add(nameof(Gunter.Data.TestCase.Filter), context.TestCase.Filter);
            table.Body.Add(nameof(Gunter.Data.TestCase.Expression), context.TestCase.Expression);
            table.Body.Add(nameof(Gunter.Data.TestCase.Assert), context.TestCase.Assert.ToString());
            table.Body.Add(nameof(Gunter.Data.TestCase.OnPassed), context.TestCase.OnPassed.ToString());
            table.Body.Add(nameof(Gunter.Data.TestCase.OnFailed), context.TestCase.OnFailed.ToString());
            table.Body.Add(nameof(Gunter.Data.TestCounter.RunTestElapsed), format($"{RuntimeVariable.TestCounter.AssertElapsed.ToString(TimespanFormat)}"));
            table.Body.Add(nameof(Gunter.Data.TestCase.Profiles), $"[{string.Join(", ", context.TestCase.Profiles.Select(p => $"'{p}'"))}]");

            return section;
        }
    }
}