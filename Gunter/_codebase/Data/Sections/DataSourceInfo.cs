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

namespace Gunter.Data.Sections
{
    public class DataSourceInfo : SectionFactory
    {
        public DataSourceInfo(ILogger logger) : base(logger) { Title = "Data-source"; }

        protected override ISection CreateCore(TestContext context, IConstantResolver constants)
        {
            var data =
                new DataTable(Title)
                .AddColumn("Property", c => c.DataType = typeof(string))
                .AddColumn("Value", c => c.DataType = typeof(string))
                .AddRow($"Query ({DataSource.CommandName.Main})", context.DataSource.ToString(DataSource.CommandName.Main, CultureInfo.InvariantCulture).Resolve(constants))
                .AddRow($"Query ({DataSource.CommandName.Debug})", context.DataSource.ToString(DataSource.CommandName.Debug, CultureInfo.InvariantCulture).Resolve(constants))
                .AddRow("Results", context.Data.Rows.Count);

            var timestampColumn = Globals.Columns.Timestamp.ToFormatString().Resolve(constants);
            if (context.Data.Columns.Contains(timestampColumn))
            {
                data.AddRow("CreatedOn", context.Data.AsEnumerable().Min(r => r.Field<DateTime>(timestampColumn)));
                var timeSpan =
                    context.Data.AsEnumerable().Max(r => r.Field<DateTime>(timestampColumn)) -
                    context.Data.AsEnumerable().Min(r => r.Field<DateTime>(timestampColumn));
                data.AddRow("TimeSpan", timeSpan.ToString(Globals.DataSourceInfo.TimeSpanFormat.ToFormatString().Resolve(constants)));
            }

            return new Section
            {
                Title = Title,
                Data = data,
                Orientation = Orientation.Vertical
            };
        }
    }

}
