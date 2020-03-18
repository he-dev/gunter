using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using Gunter.Annotations;
using Gunter.Services.Abstractions;
using Newtonsoft.Json;
using Reusable.Exceptionize;
using Reusable.Extensions;

namespace Gunter.Services.DataFilters
{
    public class ValidateService
    {
        public void Execute(object service)
        {
            Validator.ValidateObject(service, new ValidationContext(service), true);
        }
    }

    [Gunter]
    public class GetFirstLine : IFilterData
    {
        [JsonProperty(Required = Required.Always)]
        public List<DataColumnSetting>? Columns { get; set; }

        public void Execute(DataTable dataTable)
        {
            //if (Columns is null) throw new InvalidOperationException($"{nameof(GetFirstLine)}Filter requires at least one column.");
            
            var items =
                from dataRow in dataTable.InitializeTargetColumns(Columns).AsEnumerable()
                from column in Columns
                let value = dataRow.Field<object>(column.From) as string
                where value is { }
                let firstLine = value.SplitByLineBreaks().NonNullOrWhitespace().FirstOrDefault()
                where firstLine is {}
                select (dataRow, column, firstLine);

            foreach (var (dataRow, column, firstLine) in items)
            {
                dataRow[column.Into] = firstLine;
            }
        }
    }

    public static class DataTableHelper
    {
        public static DataTable InitializeTargetColumns<TColumn>(this DataTable dataTable, IEnumerable<TColumn> columns) where TColumn : DataColumnSetting
        {
            var newColumns =
                from column in columns
                where !dataTable.Columns.Contains(column.Into)
                select column;

            foreach (var column in newColumns)
            {
                dataTable.Columns.Add(new DataColumn(column.Into, typeof(object)));
            }

            return dataTable;
        }
    }

    public class DataColumnSetting
    {
        private string _into;

        /// <summary>
        /// Gets or sets the data-table column containing json.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string From { get; set; }

        /// <summary>
        /// Gets or sets the name of the column to attach.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string Into
        {
            get => _into ?? From;
            set => _into = value;
        }
    }
}