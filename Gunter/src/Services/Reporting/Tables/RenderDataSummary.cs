using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Custom;
using Gunter.Data;
using Gunter.Data.Configuration.Reporting;
using Gunter.Data.Configuration.Reports.CustomSections;
using Gunter.Extensions;
using Gunter.Services.Abstractions;
using Gunter.Workflow.Data;
using JetBrains.Annotations;
using Reusable.Exceptionize;
using Reusable.Extensions;
using Reusable.Utilities.Mailr.Models;

namespace Gunter.Services.Reporting.Tables
{
    [PublicAPI]
    public class RenderDataSummary : IRenderReportSection<DataSummary>
    {
        public RenderDataSummary(ITryGetFormatValue tryGetFormatValue, TestContext testContext)
        {
            TryGetFormatValue = tryGetFormatValue;
            TestContext = testContext;
        }

        private ITryGetFormatValue TryGetFormatValue { get; }

        private TestContext TestContext { get; }

        public IReportSectionDto Execute(DataSummary model)
        {
            // Use either custom columns or all by default.
            var columns =
                model.Columns.Any()
                    ? model.Columns.ToList()
                    : TestContext.Data.Columns.Cast<DataColumn>().Select(c => new DataColumnSetting { Select = c.ColumnName, ReduceType = ReduceType.Last }).ToList();


            var table = new HtmlTable(columns.Select(column => (column.Display, typeof(string))));

            // Filter rows before processing them.
            var dataRows = TestContext.Data.Select(TestContext.TestCase.Filter);

            // We'll use it a lot so materialize it.
            var keyColumns = columns.Where(x => x.IsKey).ToList();

            var results =
                from dataRow in dataRows
                group dataRow by dataRow.CreateKey(keyColumns) into dataRowGroup
                let result =
                    from column in columns
                    let aggregate = Aggregate(column, dataRowGroup)
                    let formatted = column.Formatter?.Execute(aggregate) ?? aggregate
                    select (column, value: formatted)
                select result;

            foreach (var row in results)
            {
                var newRow = table.Body.AddRow();
                foreach (var (column, value) in row)
                {
                    newRow.Column(column.Display).Pipe(c =>
                    {
                        c.Value = value;
                        c.Tags.UnionWith(column.Tags);
                    });
                }
            }

            // Add the footer row with column options.
            table.Foot.Add(columns.Select(column => StringifyColumnOption(column).Join(", ")));

            return ReportSectionDto.Create(model, dataSummary => new { Data = table });
        }

        private IEnumerable<string> StringifyColumnOption(DataColumnSetting columnSetting)
        {
            yield return columnSetting.IsKey ? "Key" : columnSetting.ReduceType.ToString();
        }

        private object? Aggregate(DataColumnSetting columnSetting, IEnumerable<DataRow> dataRows)
        {
            try
            {
                var values =
                    from dataRow in dataRows
                    let value = dataRow[columnSetting.Select]
                    where value != DBNull.Value && value is {}
                    select value;

                return values.Reduce(columnSetting.ReduceType);
            }
            catch (Exception inner)
            {
                throw DynamicException.Create("Aggregate", $"Could not aggregate column '{columnSetting.Select}'.", inner);
            }
        }
    }
}