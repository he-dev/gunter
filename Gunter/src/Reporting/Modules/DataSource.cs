using System;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using Gunter.Data;
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

        public DataTable Create(TestContext context)
        {
            var format = (FormatFunc)context.Formatter.Format;

            // Initialize the data-table;
            var dataTable =
                new DataTable(nameof(DataSource))
                    .AddColumn("Property", c => c.DataType = typeof(string))
                    .AddColumn("Value", c => c.DataType = typeof(string));

            dataTable.AddRow("Type", context.DataSource.GetType().Name);

            var commandNumber = 0;
            foreach (var command in context.DataSource.ToString(context.Formatter))
            {
                var commandNameOrCounter =
                    string.IsNullOrEmpty(command.Name)
                        ? commandNumber++.ToString()
                        : command.Name;

                dataTable.AddRow($"Query: {commandNameOrCounter}", command.Text);
            }

            dataTable.AddRow("RowCount", context.Data.Rows.Count);
            dataTable.AddRow("Elapsed", format($"{RuntimeVariable.TestMetrics.GetDataElapsed}:{TimespanFormat}}}"));

            var hasTimestampColumn = context.Data.Columns.Contains(TimestampColumn);
            var hasRows = context.Data.Rows.Count > 0; // If there are no rows Min/Max will throw.

            if (hasTimestampColumn && hasRows)
            {
                var timestampMin = context.Data.AsEnumerable().Min(r => r.Field<DateTime>(TimestampColumn));
                var timestampMax = context.Data.AsEnumerable().Max(r => r.Field<DateTime>(TimestampColumn));

                dataTable.AddRow("Timestamp: min", timestampMin.ToString(CultureInfo.InvariantCulture));
                dataTable.AddRow("Timestamp: max", timestampMax.ToString(CultureInfo.InvariantCulture));

                var timespan = timestampMax - timestampMin;
                dataTable.AddRow("Timespan", timespan.ToString(TimespanFormat, CultureInfo.InvariantCulture));
            }

            return dataTable;
        }
    }
}
