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

        public DataTable Create(TestUnit testUnit)
        {
            // Initialize the data-table;
            var dataTable =
                new DataTable(nameof(DataSourceInfo))
                    .AddColumn("Property", c => c.DataType = typeof(string))
                    .AddColumn("Value", c => c.DataType = typeof(string));

            dataTable.AddRow("Type", testUnit.DataSource.GetType().Name);

            var commandNumber = 0;
            foreach (var tuple in testUnit.DataSource.GetCommands())
            {
                var commandNameOrCounter =
                    string.IsNullOrEmpty(tuple.Name)
                        ? commandNumber++.ToString()
                        : tuple.Name;

                dataTable.AddRow($"Command: {commandNameOrCounter}", tuple.Text);
            }

            dataTable.AddRow("Results", testUnit.DataSource.Data.Rows.Count);
            dataTable.AddRow("Elapsed", testUnit.DataSource.Elapsed.ToString(@"hh\:mm\:ss\.f"));

            var hasTimestampColumn = testUnit.DataSource.Data.Columns.Contains(TimestampColumn);
            var hasRows = testUnit.DataSource.Data.Rows.Count > 0; // If there are no rows Min/Max will throw.

            if (hasTimestampColumn && hasRows)
            {
                var timestampMin = testUnit.DataSource.Data.AsEnumerable().Min(r => r.Field<DateTime>(TimestampColumn));
                var timestampMax = testUnit.DataSource.Data.AsEnumerable().Max(r => r.Field<DateTime>(TimestampColumn));

                dataTable.AddRow("Timestamp Min", timestampMin);
                dataTable.AddRow("Timestamp Max", timestampMax);

                var timespan = timestampMax - timestampMin;
                dataTable.AddRow("Timespan", timespan.ToString(TimespanFormat, CultureInfo.InvariantCulture));
            }

            return dataTable;
        }
    }
}
