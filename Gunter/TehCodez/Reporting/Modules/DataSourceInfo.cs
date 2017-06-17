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
            var body =
                new DataTable(nameof(DataSourceInfo))
                    .AddColumn("Property", c => c.DataType = typeof(string))
                    .AddColumn("Value", c => c.DataType = typeof(string))
                    .AddRow("Type", testUnit.DataSource.GetType().Name);

            var commandNumber = 0;
            foreach (var tuple in testUnit.DataSource.GetCommands())
            {
                body.AddRow($"Command: {(string.IsNullOrEmpty(tuple.Name) ? commandNumber++.ToString() : tuple.Name)}", tuple.Text);
            }

            body
                .AddRow("Results", testUnit.DataSource.Data.Rows.Count);

            if (testUnit.DataSource.Data.Columns.Contains(TimestampColumn) && testUnit.DataSource.Data.Rows.Count > 0)
            {
                body.AddRow("CreatedOn", testUnit.DataSource.Data.AsEnumerable().Min(r => r.Field<DateTime>(TimestampColumn)));
                var timespan =
                    testUnit.DataSource.Data.AsEnumerable().Max(r => r.Field<DateTime>(TimestampColumn)) -
                    testUnit.DataSource.Data.AsEnumerable().Min(r => r.Field<DateTime>(TimestampColumn));
                body.AddRow("Timespan", timespan.ToString(TimespanFormat, CultureInfo.InvariantCulture));
            }

            return body;
        }
    }
}
