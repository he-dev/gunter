using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Custom;
using Gunter.Data.Configuration;
using Gunter.Extensions;
using Gunter.Reporting;
using Gunter.Workflows;
using JetBrains.Annotations;
using Reusable.Collections;
using Reusable.Exceptionize;
using Reusable.Utilities.Mailr.Models;
using ReportModule = Gunter.Reporting.ReportModule;

namespace Gunter.Data.Reporting
{
    [PublicAPI]
    [Renderer(typeof(RenderDataInfo))]
    public class DataInfo : ReportModule
    {
        public TableOrientation Orientation => TableOrientation.Horizontal;

        public bool HasFoot => true;

        public List<DataInfoColumn?> Columns { get; set; } = new List<DataInfoColumn?>();
    }

    [PublicAPI]
    public class RenderDataInfo : IRenderDto
    {
        private static readonly IEqualityComparer<IEnumerable<object>> GroupKeyEqualityComparer = EqualityComparerFactory<IEnumerable<object>>.Create
        (
            (left, right) => left.SequenceEqual(right),
            (keys) => keys.CalcHashCode()
        );

        private delegate object? AggregateCallback(IEnumerable<object?> values);

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

        public RenderDataInfo(Format format, TestContext testContext)
        {
            Format = format;
            TestContext = testContext;
        }

        private Format Format { get; }

        private TestContext TestContext { get; }

        public IReportModule Execute(ReportModule model) => Execute(model as DataInfo);

        private IReportModule Execute(DataInfo model)
        {
            // Materialize it because we'll be modifying it.
            var columns = model.Columns.ToList();

            // Use all columns by default if none-specified.
            if (!model.Columns.Any())
            {
                columns = TestContext.Data.Columns.Cast<DataColumn>().Select(c => new DataInfoColumn
                {
                    Select = c.ColumnName,
                    Aggregate = ColumnAggregate.Last
                }).ToList();
            }

            var section = new ReportModule<DataInfo>
            {
                Heading = model.Heading.FormatWith(Format),
                Data = new HtmlTable(HtmlTableColumn.Create(columns.Select(column => ((column.Display ?? column.Select).ToString(), typeof(string))).ToArray()))
            };

            // Filter rows before processing them.
            var filteredRows = TestContext.Data.Select(TestContext.TestCase.Filter);

            // We'll use it a lot so materialize it.
            var groupColumns = columns.Where(x => x.IsKey).ToList();
            var rowGroups = filteredRows.GroupBy(row => row.GroupKey(groupColumns), GroupKeyEqualityComparer);

            // Create aggregated rows and add them to the final data-table.            
            var aggregatedRows =
                from rowGroup in rowGroups
                let aggregated = from column in columns select (column, value: Aggregate(column, rowGroup))
                select aggregated;

            foreach (var row in aggregatedRows)
            {
                var newRow = section.Data.Body.NewRow();
                foreach (var (column, value) in row)
                {
                    newRow.Update((column.Display ?? column.Select).ToString(), value, column.Styles);
                }
            }

            // Add the footer row with column options.
            section.Data.Foot.Add(columns.Select(column => string.Join(", ", StringifyColumnOption(column))).ToList());

            return section;
        }

        private IEnumerable<string> StringifyColumnOption(DataInfoColumn column)
        {
            yield return column.IsKey ? "Key" : column.Aggregate.ToString();
        }

        private object Aggregate(DataInfoColumn column, IEnumerable<DataRow> rowGroup)
        {
            try
            {
                var aggregate = Aggregates[column.Aggregate];
                var values = rowGroup.Values((string)column.Select).NotDBNull();
                if (aggregate(values) is {} value)
                {
                    return column.Formatter?.Apply(value) ?? value;
                }
            }
            catch (Exception inner)
            {
                throw DynamicException.Create("Aggregate", $"Could not aggregate '{column.Select.ToString()}'.", inner);
            }

            return default;
        }
    }

    internal static class DataRowExtensions
    {
        /// <summary>
        /// Creates a group-key for the specified row.
        /// </summary>
        public static IEnumerable<object> GroupKey(this DataRow dataRow, IEnumerable<DataInfoColumn> keyColumns)
        {
            // Get key values and apply their filters.
            //return keyColumns.Select(column => column.Filter.Apply(dataRow[column.Name.ToString()]));
            return keyColumns.Select(column => dataRow[column.Select.ToString()]);
        }
    }
}