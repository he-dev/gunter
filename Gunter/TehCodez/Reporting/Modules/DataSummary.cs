using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Gunter.Data;
using Gunter.Extensions;
using Gunter.Reporting.Data;
using Gunter.Reporting.Filters;
using JetBrains.Annotations;
using Reusable.Data;

namespace Gunter.Reporting.Modules
{
    [PublicAPI]
    public class DataSummary : Module, ITabular
    {
        private delegate object AggregateCallback([NotNull, ItemCanBeNull] IEnumerable<object> values);

        private static readonly Dictionary<ColumnTotal, AggregateCallback> Aggregates = new Dictionary<ColumnTotal, AggregateCallback>
        {
            [ColumnTotal.First] = values => values.FirstOrDefault(),
            [ColumnTotal.Last] = values => values.LastOrDefault(),
            [ColumnTotal.Min] = values => values.Select(Convert.ToDouble).AggregateOrDefault(Enumerable.Min, double.NaN),
            [ColumnTotal.Max] = values => values.Select(Convert.ToDouble).AggregateOrDefault(Enumerable.Max, double.NaN),
            [ColumnTotal.Count] = values => values.Count(),
            [ColumnTotal.Sum] = values => values.Select(Convert.ToDouble).Sum(),
            [ColumnTotal.Average] = values => values.Select(Convert.ToDouble).AggregateOrDefault(Enumerable.Average, double.NaN),
        };

        public TableOrientation Orientation => TableOrientation.Horizontal;

        public bool HasFooter => true;

        [DefaultValue(true)]
        public bool IncludeGroupCount { get; set; } = true;

        [NotNull]
        [ItemCanBeNull]
        public List<ColumnOption> Columns { get; set; } = new List<ColumnOption>();

        public DataTable Create(TestUnit testUnit)
        {
            var columns = Columns.ToList();

            if (!Columns.Any())
            {
                // Use default columns if none-specified.
                columns = testUnit.DataSource.Data.Columns.Cast<DataColumn>().Select(c => new ColumnOption
                {
                    Name = c.ColumnName,
                    Total = ColumnTotal.Last
                }).ToList();
            }

            if (IncludeGroupCount)
            {
                columns.Add(ColumnOption.GroupCount);
            }

            // Group-by keyed columns.
            var dataRows = testUnit.DataSource.Data.Select(testUnit.TestCase.Filter);

            var keyColumns = columns.Where(x => x.IsKey).ToList();
            var rowGroups =
                from dataRow in dataRows
                let values = keyColumns.Select(column => column.Filter.Apply(dataRow[column.Name]))
                group dataRow by new CompositeKey<object>(values) into g
                select g;

            // Create the data-table.
            var dataTable = new DataTable(nameof(DataSummary));
            foreach (var column in columns)
            {
                dataTable.AddColumn(column.Name, c => c.DataType = typeof(string));
            }

            // Create aggregated rows and add them to the final data-table.
            var rows = rowGroups.Select(rowGroup => AggregateRows(dataTable, columns, rowGroup.ToList()));
            foreach (var row in rows)
            {
                dataTable.Rows.Add(row);
            }

            // Add the footer row with column options.
            IEnumerable<string> StringifyColumnOption(ColumnOption column)
            {
                if (column.IsKey) yield return "Key";
                if (column.Filter != null && !(column.Filter is Unchanged)) yield return column.Filter.GetType().Name;
                yield return column.Total.ToString();
            }
            dataTable.AddRow(columns.Select(column => (object)string.Join(", ", StringifyColumnOption(column))).ToArray());

            return dataTable;
        }

        private DataRow AggregateRows(DataTable dataTable, IEnumerable<ColumnOption> columns, ICollection<DataRow> rowGroup)
        {
            var dataRow = dataTable.NewRow();

            foreach (var column in columns.Where(c => !c.Equals(ColumnOption.GroupCount)))
            {
                var aggregate = Aggregates[column.Total];
                var values = rowGroup.Select(column).NotDBNull();
                var value = aggregate(values);
                dataRow[column.Name] = column.Filter == null ? value : column.Filter.Apply(value);
            }

            if (IncludeGroupCount)
            {
                dataRow[ColumnOption.GroupCount] = rowGroup.Count();
            }
            return dataRow;
        }
    }

    // Represents an aggregated column and its options.

    //public class CollectionComparer<TValue> : IEqualityComparer<IEnumerable<TValue>>
    //{
    //    public bool Equals(IEnumerable<TValue> x, IEnumerable<TValue> y)
    //    {
    //        if (ReferenceEquals(x, null)) return false;
    //        if (ReferenceEquals(y, null)) return false;
    //        return ReferenceEquals(x, y) || x.SequenceEqual(y);
    //    }

    //    public int GetHashCode(IEnumerable<TValue> obj)
    //    {
    //        unchecked
    //        {
    //            return obj.Aggregate(0, (current, next) => (current * 397) ^ next?.GetHashCode() ?? 0);
    //        }
    //    }
    //}
}
