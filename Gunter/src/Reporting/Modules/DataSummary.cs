using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Linq.Custom;
using System.Text;
using System.Text.RegularExpressions;
using Gunter.Data;
using Gunter.Data.Dtos;
using Gunter.Extensions;
using Gunter.Reporting.Filters;
using JetBrains.Annotations;
using Reusable.Collections;
using Reusable.Data;
using Reusable.Extensions;

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

        public bool HasFoot => true;

        [DefaultValue(true)]
        public bool HasGroupCount { get; set; } = true;

        [NotNull]
        [ItemCanBeNull]
        public List<ColumnOption> Columns { get; set; } = new List<ColumnOption>();

        public override SectionDto CreateDto(TestContext context)
        {
            // Materialize it because we'll be modifying it.
            var columns = Columns.ToList();

            if (!Columns.Any())
            {
                // Use default columns if none-specified.
                columns = context.Data.Columns.Cast<DataColumn>().Select(c => new ColumnOption
                {
                    Name = c.ColumnName,
                    Total = ColumnTotal.Last
                })
                .ToList();
            }

            if (HasGroupCount)
            {
                columns.Add(ColumnOption.GroupCount);
            }

            // Filter rows before processing them.
            var dataRows = context.Data.Select(context.TestCase.Filter);

            // We'll use it a lot so materialize it.
            var keyColumns = columns.Where(x => x.IsKey).ToList();

            var keyEqualityComparer = EqualityComparerFactory<IEnumerable<object>>.Create(
                (left, right) => left.SequenceEqual(right),
                (keys) => keys.CalcHashCode()
            );            

            //var rowGroups =
            //    from dataRow in dataRows
            //    group dataRow by keyEqualityComparer into g
            //    select g;

            var rowGroups = dataRows.GroupBy(row => row.Keys(keyColumns), keyEqualityComparer);

            // Create the data-table.
            //var dataTable = new DataTable(nameof(DataSummary));
            var section = new SectionDto
            {
                Table = new TripleTableDto(columns.Select(c => c.Name.ToString()))
            };
            var table = section.Table;                       

            // Create aggregated rows and add them to the final data-table.
            var rows = rowGroups.Select(rowGroup => AggregateRows(columns, rowGroup));
            foreach (var row in rows)
            {
                table.Body.Add(row);
            }

            // Add the footer row with column options.
            table.Foot.Add(columns.Select(column => string.Join(", ", StringifyColumnOption(column))).ToList());

            return section;
            
            IEnumerable<string> StringifyColumnOption(ColumnOption column)
            {
                if (column.IsKey) yield return "Key";
                if (column.Filter != null && !(column.Filter is Unchanged)) yield return column.Filter.GetType().Name;
                yield return column.Total.ToString();
            }
        }

        private List<string> AggregateRows(IEnumerable<ColumnOption> columns, IGrouping<IEnumerable<object>, DataRow> rowGroup)
        {
            var row = new List<string>();
            foreach (var column in columns.Where(c => !c.Equals(ColumnOption.GroupCount)))
            {
                var aggregate = Aggregates[column.Total];
                var values = rowGroup.Values(column).NotDBNull();
                var value = aggregate(values);
                value = column.Filter is null ? value : column.Filter.Apply(value);
                value = column.Formatter is null ? value : column.Formatter.Apply(value);
                row.Add(value?.ToString());
            }

            if (HasGroupCount)
            {
                row.Add(rowGroup.Count().ToString());
            }
            return row;
        }
    }

    internal static class DataRowExtensions
    {
        public static IEnumerable<object> Keys(this DataRow dataRow, IEnumerable<ColumnOption> keyColumns)
        {
            // Get key values and apply their filters.
            return keyColumns.Select(column => column.Filter.Apply(dataRow[column.Name.ToString()]));
        }
    }
}
