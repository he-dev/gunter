using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Custom;
using Gunter.Annotations;
using Gunter.Data.Configuration;
using Gunter.Data.Configuration.Reporting;
using Gunter.Extensions;
using Gunter.Services.Abstractions;
using Gunter.Workflow.Data;
using JetBrains.Annotations;
using Reusable.Collections;
using Reusable.Exceptionize;
using Reusable.Utilities.Mailr.Models;
using ReportModule = Gunter.Data.Configuration.ReportModule;

namespace Gunter.Services.Reporting
{
    [PublicAPI]
    public class RenderDataSummary : IRenderReportModule
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

        public RenderDataSummary(Format format, TestContext testContext)
        {
            Format = format;
            TestContext = testContext;
        }

        private Format Format { get; }

        private TestContext TestContext { get; }

        public IReportModuleDto Execute(ReportModule module) => Execute(module as DataSummary);

        private IReportModuleDto Execute(DataSummary model)
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


            // Filter rows before processing them.
            var filteredRows = TestContext.Data.Select(TestContext.TestCase.Filter);

            // We'll use it a lot so materialize it.
            var groupColumns = columns.Where(x => x.IsKey).ToList();
            var rowGroups = filteredRows.GroupBy(row => row.GroupKey(groupColumns), GroupKeyEqualityComparer);

            // Create aggregated rows and add them to the final data-table.            
            var aggregatedRows =
                from rowGroup in rowGroups
                let aggregated = 
                    from column in columns 
                    select (column, value: Aggregate(column, rowGroup))
                select aggregated;

            var table = new HtmlTable(columns.Select(column => ((column.Display ?? column.Select).ToString(), typeof(string))));

            foreach (var row in aggregatedRows)
            {
                var newRow = table.Body.AddRow();
                foreach (var (column, value) in row)
                {
                    newRow.Set((column.Display ?? column.Select).ToString(), value, column.Styles);
                }
            }

            // Add the footer row with column options.
            table.Foot.Add(columns.Select(column => string.Join(", ", StringifyColumnOption(column))).ToList());

            //return section;
            return new ReportModuleDto<DataSummary>(model, dataSummary => new
            {
                Data = table
            });
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
}