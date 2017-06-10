using System;
using System.Data;
using System.Globalization;
using System.Linq;
using Gunter.Data;
using Gunter.Extensions;
using Reusable.Data;

namespace Gunter.Reporting.Details
{
    public class DataSourceInfo : ISectionDetail
    {
        public TableOrientation Orientation => TableOrientation.Vertical;

        public string TimestampColumn { get; set; } = "Timestamp";

        public string TimeSpanFormat { get; set; } = @"dd\.hh\:mm\:ss";

        public DataSet Create(TestContext context)
        {
            var body =
                new DataTable(nameof(DataSourceInfo))
                    .AddColumn("Property", c => c.DataType = typeof(string))
                    .AddColumn("Value", c => c.DataType = typeof(string))
                    .AddRow($"Query ({CommandName.Main})", context.DataSource.ToString(CommandName.Main, CultureInfo.InvariantCulture).Resolve(context.Constants))
                    .AddRow($"Query ({CommandName.Debug})", context.DataSource.ToString(CommandName.Debug, CultureInfo.InvariantCulture).Resolve(context.Constants))
                    .AddRow("Results", context.Data.Rows.Count);

            //var timestampColumn = VariableName.Column.Timestamp.ToFormatString().Resolve(context.Constants);
            if (context.Data.Columns.Contains(TimestampColumn) && context.Data.Rows.Count > 0)
            {
                body.AddRow("CreatedOn", context.Data.AsEnumerable().Min(r => r.Field<DateTime>(TimestampColumn)));
                var timeSpan =
                    context.Data.AsEnumerable().Max(r => r.Field<DateTime>(TimestampColumn)) -
                    context.Data.AsEnumerable().Min(r => r.Field<DateTime>(TimestampColumn));
                body.AddRow("TimeSpan", TimeSpanFormat);
            }

            return new DataSet { Tables = { body } };
        }
    }

}
