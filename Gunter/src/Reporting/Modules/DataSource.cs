using System;
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
    public class DataSource : Module, ITabular
    {
        public TableOrientation Orientation => TableOrientation.Vertical;

        public bool HasFoot => false;

        [DefaultValue("Timestamp")]
        public string TimestampColumn { get; set; }

        // Custom TimeSpan Format Strings https://msdn.microsoft.com/en-us/library/ee372287(v=vs.110).aspx

        [DefaultValue(@"mm\:ss\.fff")]
        public string TimespanFormat { get; set; }

        public override SectionDto CreateDto(TestContext context)
        {
            var format = (FormatFunc)context.Formatter.Format;

            // Initialize the data-table;
            var section = new SectionDto
            {
                Table = new TripleTableDto(new[] { "Property", "Value" })
            };
            var table = section.Table;

            table.Body.Add("Type", context.DataSource.GetType().Name);
            table.Body.Add("Query", context.DataSource.ToString(context.Formatter));
            table.Body.Add("RowCount", context.Data.Rows.Count.ToString());
            table.Body.Add("Elapsed", format($"{RuntimeVariable.TestCounter.GetDataElapsed.ToString(TimespanFormat)}"));

            var hasTimestampColumn = context.Data.Columns.Contains(TimestampColumn);
            var hasRows = context.Data.Rows.Count > 0; // If there are no rows Min/Max will throw.

            if (hasTimestampColumn && hasRows)
            {
                var timestampMin = context.Data.AsEnumerable().Min(r => r.Field<DateTime>(TimestampColumn));
                var timestampMax = context.Data.AsEnumerable().Max(r => r.Field<DateTime>(TimestampColumn));

                table.Body.Add(new List<string> { "Timestamp: min", timestampMin.ToString(CultureInfo.InvariantCulture) });
                table.Body.Add(new List<string> { "Timestamp: max", timestampMax.ToString(CultureInfo.InvariantCulture) });

                var timespan = timestampMax - timestampMin;
                table.Body.Add(new List<string> { "Timespan", timespan.ToString(TimespanFormat, CultureInfo.InvariantCulture) });
            }

            return section;
        }
    }
}