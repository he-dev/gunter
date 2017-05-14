using Gunter.Data;
using Gunter.Data.Sections;
using Gunter.Data.SqlClient;
using Gunter.Extensions;
using Gunter.Services;
using Reusable.Data;
using Reusable.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Gunter.Alerts.Sections
{
    public class DataSourceInfo : SectionFactory
    {
        public DataSourceInfo(ILogger logger) : base(logger) { Heading = "Data-source"; }

        protected override ISection CreateCore(TestContext context)
        {
            var body =
                new DataTable(Heading)
                .AddColumn("Property", c => c.DataType = typeof(string))
                .AddColumn("Value", c => c.DataType = typeof(string))
                .AddRow($"Query ({DataSource.CommandName.Main})", context.DataSource.ToString(DataSource.CommandName.Main, CultureInfo.InvariantCulture).Resolve(context.Constants))
                .AddRow($"Query ({DataSource.CommandName.Debug})", context.DataSource.ToString(DataSource.CommandName.Debug, CultureInfo.InvariantCulture).Resolve(context.Constants))
                .AddRow("Results", context.Data.Rows.Count);

            var timestampColumn = VariableName.Column.Timestamp.ToFormatString().Resolve(context.Constants);
            if (context.Data.Columns.Contains(timestampColumn))
            {
                body.AddRow("CreatedOn", context.Data.AsEnumerable().Min(r => r.Field<DateTime>(timestampColumn)));
                var timeSpan =
                    context.Data.AsEnumerable().Max(r => r.Field<DateTime>(timestampColumn)) -
                    context.Data.AsEnumerable().Min(r => r.Field<DateTime>(timestampColumn));
                body.AddRow("TimeSpan", timeSpan.ToString(VariableName.DataSourceInfo.TimeSpanFormat.ToFormatString().Resolve(context.Constants)));
            }

            return new TableSection
            {
                Heading = Heading,
                Body = body,
                Orientation = Orientation.Vertical
            };
        }
    }

}
