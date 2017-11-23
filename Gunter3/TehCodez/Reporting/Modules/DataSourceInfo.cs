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
    public class DataSourceInfo : Module, ITabular
    {
        public TableOrientation Orientation => TableOrientation.Vertical;

        public bool HasFooter => false;

        [PublicAPI]
        [DefaultValue("Timestamp")]
        public string TimestampColumn { get; set; }

        // Custom TimeSpan Format Strings https://msdn.microsoft.com/en-us/library/ee372287(v=vs.110).aspx

        [PublicAPI]
        [DefaultValue(@"dd\.hh\:mm\:ss")]
        public string TimespanFormat { get; set; }

        public DataTable Create(TestContext context)
        {
            // Initialize the data-table;
            var dataTable =
                new DataTable(nameof(DataSourceInfo))
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

                dataTable.AddRow($"Command: {commandNameOrCounter}", command.Text);
            }

            dataTable.AddRow("Results", context.Data.Rows.Count);
            dataTable.AddRow("Elapsed", context.GetDataElapsed.ToString(@"hh\:mm\:ss\.f")); // todo hardcoded timespan format

            var hasTimestampColumn = context.Data.Columns.Contains(TimestampColumn);
            var hasRows = context.Data.Rows.Count > 0; // If there are no rows Min/Max will throw.

            if (hasTimestampColumn && hasRows)
            {
                var timestampMin = context.Data.AsEnumerable().Min(r => r.Field<DateTime>(TimestampColumn));
                var timestampMax = context.Data.AsEnumerable().Max(r => r.Field<DateTime>(TimestampColumn));

                dataTable.AddRow("Timestamp Min", timestampMin);
                dataTable.AddRow("Timestamp Max", timestampMax);

                var timespan = timestampMax - timestampMin;
                dataTable.AddRow("Timespan", timespan.ToString(TimespanFormat, CultureInfo.InvariantCulture));
            }

            return dataTable;
        }
    }
}
