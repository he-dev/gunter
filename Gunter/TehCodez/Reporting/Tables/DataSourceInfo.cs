using System;
using System.Data;
using System.Globalization;
using System.Linq;
using Gunter.Data;
using Gunter.Extensions;
using Reusable.Data;

namespace Gunter.Reporting.Tables
{
    public class DataSourceInfo : ISectionDetail
    {
        public TableOrientation Orientation => TableOrientation.Vertical;

        public DataSet CreateDetail(TestContext context)
        {
            var body =
                new DataTable(nameof(DataSourceInfo))
                    .AddColumn("Property", c => c.DataType = typeof(string))
                    .AddColumn("Value", c => c.DataType = typeof(string))
                    .AddRow($"Query ({CommandName.Main})", context.DataSource.ToString(CommandName.Main, CultureInfo.InvariantCulture).Resolve(context.Constants))
                    .AddRow($"Query ({CommandName.Debug})", context.DataSource.ToString(CommandName.Debug, CultureInfo.InvariantCulture).Resolve(context.Constants))
                    .AddRow("Results", context.Data.Rows.Count);

            var timestampColumn = VariableName.Column.Timestamp.ToFormatString().Resolve(context.Constants);
            if (context.Data.Columns.Contains(timestampColumn) && context.Data.Rows.Count > 0)
            {
                body.AddRow("CreatedOn", context.Data.AsEnumerable().Min(r => r.Field<DateTime>(timestampColumn)));
                var timeSpan =
                    context.Data.AsEnumerable().Max(r => r.Field<DateTime>(timestampColumn)) -
                    context.Data.AsEnumerable().Min(r => r.Field<DateTime>(timestampColumn));
                body.AddRow("TimeSpan", timeSpan.ToString(VariableName.DataSourceInfo.TimeSpanFormat.ToFormatString().Resolve(context.Constants)));
            }

            return new DataSet { Tables = { body } };
        }
    }

}
