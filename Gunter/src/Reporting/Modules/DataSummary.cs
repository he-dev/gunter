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
using Gunter.Services;
using JetBrains.Annotations;
using Reusable.Collections;
using Reusable.Data;
using Reusable.Exceptionizer;
using Reusable.Extensions;
using Reusable.IOnymous.Models;
using Reusable.Reflection;

namespace Gunter.Reporting.Modules
{
    [PublicAPI]
    public class DataSummary : Module, ITabular
    {
        private static readonly IEqualityComparer<IEnumerable<object>> GroupKeyEqualityComparer =
            EqualityComparerFactory<IEnumerable<object>>.Create(
                (left, right) => left.SequenceEqual(right),
                (keys) => keys.CalcHashCode()
            );

        private delegate object AggregateCallback([NotNull, ItemCanBeNull] IEnumerable<object> values);

        private static readonly Dictionary<ColumnAggregate, AggregateCallback> Aggregates = new Dictionary<ColumnAggregate, AggregateCallback>
        {
            [ColumnAggregate.First] = values => values.FirstOrDefault(),
            [ColumnAggregate.Last] = values => values.LastOrDefault(),
            [ColumnAggregate.Min] = values => values.Select(Convert.ToDouble).AggregateOrDefault(Enumerable.Min, double.NaN),
            [ColumnAggregate.Max] = values => values.Select(Convert.ToDouble).AggregateOrDefault(Enumerable.Max, double.NaN),
            [ColumnAggregate.Count] = values => values.Count(),
            [ColumnAggregate.Sum] = values => values.Select(Convert.ToDouble).Sum(),
            [ColumnAggregate.Average] = values => values.Select(Convert.ToDouble).AggregateOrDefault(Enumerable.Average, double.NaN),
        };

        public TableOrientation Orientation => TableOrientation.Horizontal;

        public bool HasFoot => true;

        [NotNull]
        [ItemCanBeNull]
        public List<ColumnMetadata> Columns { get; set; } = new List<ColumnMetadata>();

        public override ModuleDto CreateDto(TestContext context)
        {
            // Materialize it because we'll be modifying it.
            var columns = Columns.ToList();

            // Use all columns by default if none-specified.
            if (!Columns.Any())
            {
                columns = context.Data.Columns.Cast<DataColumn>().Select(c => new ColumnMetadata
                    {
                        Select = c.ColumnName,
                        Aggregate = ColumnAggregate.Last
                    })
                    .ToList();
            }

            var section = new ModuleDto
            {
                Heading = Heading.Format(context.RuntimeVariables),
                Data = new HtmlTable(HtmlTableColumn.Create(columns.Select(column => ((column.Display ?? column.Select).ToString(), typeof(string))).ToArray()))
            };
            //var table = section.Data;

            // Filter rows before processing them.
            var filteredRows = context.Data.Select(context.TestCase.Filter);

            // We'll use it a lot so materialize it.
            var groupColumns = columns.Where(x => x.IsKey).ToList();
            var rowGroups = filteredRows.GroupBy(row => row.GroupKey(groupColumns), GroupKeyEqualityComparer);

            // Create aggregated rows and add them to the final data-table.            
            var aggregatedRows =
                from rowGroup in rowGroups
                select (from column in columns select Aggregate(column, rowGroup));

            foreach (var row in aggregatedRows)
            {
                section.Data.Body.Add(row);
            }

            // Add the footer row with column options.
            section.Data.Foot.Add(columns.Select(column => string.Join(", ", StringifyColumnOption(column))).ToList());

            return section;

            IEnumerable<string> StringifyColumnOption(ColumnMetadata column)
            {
                if (column.IsKey) yield return "Key";
                //if (column.Filter != null && !(column.Filter is Unchanged)) yield return column.Filter.GetType().Name;
                yield return column.Aggregate.ToString();
            }
        }

        private string Aggregate(ColumnMetadata column, IEnumerable<DataRow> rowGroup)
        {
            try
            {
                var aggregate = Aggregates[column.Aggregate];
                var values = rowGroup.Values((string)column.Select).NotDBNull();
                var value = aggregate(values);
                if (value is null)
                {
                    return default;
                }
                else
                {
                    //value = column.Filter is null ? value : column.Filter.Apply(value);
                    value = column.Formatter is null ? value : column.Formatter.Apply(value);
                    return value.ToString();
                }
            }
            catch (Exception inner)
            {
                throw DynamicException.Create("Aggregate", $"Could not aggregate '{column.Select.ToString()}'.", inner);
            }
        }
    }

    internal static class DataRowExtensions
    {
        /// <summary>
        /// Creates a group-key for the specified row.
        /// </summary>
        public static IEnumerable<object> GroupKey(this DataRow dataRow, IEnumerable<ColumnMetadata> keyColumns)
        {
            // Get key values and apply their filters.
            //return keyColumns.Select(column => column.Filter.Apply(dataRow[column.Name.ToString()]));
            return keyColumns.Select(column => dataRow[column.Select.ToString()]);
        }
    }
}