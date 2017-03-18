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
            var data = context.DataSource.GetData(context.Constants);

            var body =
                new DataTable(Heading)
                .AddColumn("Property", c => c.DataType = typeof(string))
                .AddColumn("Value", c => c.DataType = typeof(string))
                .AddRow($"Query ({DataSource.CommandName.Main})", context.DataSource.ToString(DataSource.CommandName.Main, CultureInfo.InvariantCulture).Resolve(context.Constants))
                .AddRow($"Query ({DataSource.CommandName.Debug})", context.DataSource.ToString(DataSource.CommandName.Debug, CultureInfo.InvariantCulture).Resolve(context.Constants))
                .AddRow("Results", data.Rows.Count);

            var timestampColumn = Globals.Columns.Timestamp.ToFormatString().Resolve(context.Constants);
            if (data.Columns.Contains(timestampColumn))
            {
                body.AddRow("CreatedOn", data.AsEnumerable().Min(r => r.Field<DateTime>(timestampColumn)));
                var timeSpan =
                    data.AsEnumerable().Max(r => r.Field<DateTime>(timestampColumn)) -
                    data.AsEnumerable().Min(r => r.Field<DateTime>(timestampColumn));
                body.AddRow("TimeSpan", timeSpan.ToString(Globals.DataSourceInfo.TimeSpanFormat.ToFormatString().Resolve(context.Constants)));
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
