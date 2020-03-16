using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Gunter.Data;
using Gunter.Data.Configuration;
using Gunter.Data.Configuration.Reporting;
using Gunter.Services.Abstractions;
using Gunter.Workflow.Data;
using Reusable.Extensions;
using Reusable.Utilities.Mailr.Models;

namespace Gunter.Services.Reporting
{
    using static RenderQuerySummary.Columns;
    public class RenderQuerySummary : IRenderReportModule
    {
        public RenderQuerySummary(Format format, TestContext context)
        {
            Format = format;
            Context = context;
        }

        private Format Format { get; }

        private TestContext Context { get; }

        public IReportModuleDto Execute(ReportModule model) => Execute(model as QuerySummary);

        private IReportModuleDto Execute(QuerySummary model)
        {
            var table = new HtmlTable
            (
                ("Property", typeof(string)),
                ("Value", typeof(string))
            );

            table.Body.Add("Type", Context.Query.GetType().Name);
            table.Body.AddRow().Set(Property, "Command").Set(Value, Context.Query, "query");
            table.Body.Add("Results", Context.Data.Rows.Count.ToString());
            table.Body.Add("Elapsed", $"{{{nameof(TestContext)}.{nameof(TestContext.GetDataElapsed)}:{model.TimespanFormat}}}".Map(Format));

            var hasTimestampColumn = Context.Data.Columns.Contains(model.TimestampColumn);
            var hasRows = Context.Data.Rows.Count > 0; // If there are no rows Min/Max will throw.

            if (hasTimestampColumn && hasRows)
            {
                var timestampMin = Context.Data.AsEnumerable().Min(r => r.Field<DateTime>(model.TimestampColumn));
                var timestampMax = Context.Data.AsEnumerable().Max(r => r.Field<DateTime>(model.TimestampColumn));

                table.Body.Add(new List<string> { "Timestamp: min", timestampMin.ToString(CultureInfo.InvariantCulture) });
                table.Body.Add(new List<string> { "Timestamp: max", timestampMax.ToString(CultureInfo.InvariantCulture) });

                var timespan = timestampMax - timestampMin;
                table.Body.Add(new List<string> { "Timespan", timespan.ToString(model.TimespanFormat, CultureInfo.InvariantCulture) });
            }

            return new ReportModuleDto<QuerySummary>(model, _ => new
            {
                Data = table
            });
        }

        public static class Columns
        {
            public const string Property = nameof(Property);

            public const string Value = nameof(Value);
        }
    }
}