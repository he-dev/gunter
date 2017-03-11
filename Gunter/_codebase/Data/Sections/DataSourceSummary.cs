using Gunter.Data.SqlClient;
using Gunter.Services;
using Reusable.Data;
using Reusable.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Gunter.Data.Sections
{
    public class DataSourceSummary : SectionFactory
    {
        public DataSourceSummary(ILogger logger) : base(logger) { }

        protected override ISection CreateCore(TestContext context, IConstantResolver constants)
        {
            var data = new DataTable("Data source")
                .AddColumn("Property", c => c.DataType = typeof(string))
                .AddColumn("Value", c => c.DataType = typeof(string));

            var timestampColumn = constants.Resolve(Globals.Columns.Timestamp);

            data.AddRow($"Query ({DataSource.CommandName.Main})", context.DataSource.ToString(DataSource.CommandName.Main, CultureInfo.InvariantCulture));
            data.AddRow($"Query ({DataSource.CommandName.Debug})", context.DataSource.ToString(DataSource.CommandName.Debug, CultureInfo.InvariantCulture));
            data.AddRow("Results", context.Data.Rows.Count);
            data.AddRow("CreatedOn", context.Data.AsEnumerable().Min(r => r.Field<DateTime>(timestampColumn)));
            data.AddRow("TimeSpan",
                context.Data.AsEnumerable().Max(r => r.Field<DateTime>(timestampColumn)) -
                context.Data.AsEnumerable().Min(r => r.Field<DateTime>(timestampColumn)));

            return new Section
            {
                Heading = "Data source",
                Data = data,
                Orientation = Orientation.Horizontal
            };
        }
    }

}
