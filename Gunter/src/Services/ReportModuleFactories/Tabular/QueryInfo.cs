using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using Gunter.Data;
using Gunter.Workflows;
using Reusable.Extensions;
using Reusable.Utilities.Mailr.Models;

namespace Gunter.Reporting.Modules.Tabular
{
    [Renderer(typeof(QueryInfo))]
    public class QueryInfo : ReportModule
    {
        public TableOrientation Orientation => TableOrientation.Vertical;

        public bool HasFoot => false;

        [DefaultValue("Timestamp")]
        public string TimestampColumn { get; set; }

        // Custom TimeSpan Format Strings https://msdn.microsoft.com/en-us/library/ee372287(v=vs.110).aspx

        [DefaultValue(@"mm\:ss\.fff")]
        public string TimespanFormat { get; set; }
        
    }
    
    public class RenderQueryInfo : IRenderDto
    {
        public RenderQueryInfo(Format format, TestContext context)
        {
            Format = format;
            Context = context;
        }

        private Format Format { get; }

        private TestContext Context { get; }

        public IReportModule Execute(ReportModule model) => Execute(model as QueryInfo);

        private IReportModule Execute(QueryInfo model)
        {
            // Initialize the data-table;
            var section = new ReportModule<QueryInfo>
            {
                Heading = model.Heading.FormatWith(Format),
                Data = new HtmlTable(HtmlTableColumn.Create
                (
                    ("Property", typeof(string)),
                    ("Value", typeof(string))
                ))
            };
            var table = section.Data;

            table.Body.Add("Type", Context.Query.GetType().Name);
            table.Body.NewRow().Update(Columns.Property, "Command").Update(Columns.Value, Context.Query, "query");
            table.Body.Add("Results", Context.Data.Rows.Count.ToString());
            table.Body.Add("Elapsed", $"{RuntimeProperty.BuiltIn.TestContext.GetDataElapsed.ToFormatString(TimespanFormat)}".Format(Context.Container));

            var hasTimestampColumn = Context.Data.Columns.Contains(TimestampColumn);
            var hasRows = Context.Data.Rows.Count > 0; // If there are no rows Min/Max will throw.

            if (hasTimestampColumn && hasRows)
            {
                var timestampMin = Context.Data.AsEnumerable().Min(r => r.Field<DateTime>(TimestampColumn));
                var timestampMax = Context.Data.AsEnumerable().Max(r => r.Field<DateTime>(TimestampColumn));

                table.Body.Add(new List<string> { "Timestamp: min", timestampMin.ToString(CultureInfo.InvariantCulture) });
                table.Body.Add(new List<string> { "Timestamp: max", timestampMax.ToString(CultureInfo.InvariantCulture) });

                var timespan = timestampMax - timestampMin;
                table.Body.Add(new List<string> { "Timespan", timespan.ToString(TimespanFormat, CultureInfo.InvariantCulture) });
            }

            return section;
        }

        private static class Columns
        {
            public const string Property = nameof(Property);

            public const string Value = nameof(Value);
        }

    }
}