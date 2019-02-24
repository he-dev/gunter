using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Gunter.Data;
using Gunter.Reporting.Filters.Abstractions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable.Exceptionizer;
using Reusable.Extensions;

namespace Gunter.Services.DataPostProcessors
{
    public class ExtractFirstLine : IDataPostProcessor
    {
        [NotNull, ItemNotNull]
        [JsonProperty(Required = Required.Always)]
        public IList<ExtractFirstLineColumn> Columns { get; set; }

        public void Execute(DataTable dataTable)
        {
            if (Columns is null) throw new InvalidOperationException($"There are no '{nameof(Columns)}'.");

            foreach (var column in Columns.Where(c => c.Attach.IsNotNullOrEmpty()))
            {
                if (dataTable.Columns.Contains(column.Attach))
                {
                    throw DynamicException.Create("ColumnAlreadyExists", $"The data-table already contains a column with the name '{column.Attach}'.");
                }

                dataTable.Columns.Add(new DataColumn(column.Attach, typeof(object)));
            }

            foreach (var column in Columns)
            {
                foreach (var dataRow in dataTable.AsEnumerable())
                {
                    try
                    {
                        var firstLine = GetFirstLine(dataRow.Field<object>(column.From));
                        dataRow[column.Attach ?? column.From] = firstLine;
                    }
                    catch (Exception inner)
                    {
                        throw DynamicException.Create("AttachmentCompute", $"Could not compute the '{column.Attach ?? column.From}' attachment.", inner);
                    }
                }
            }
        }

        [CanBeNull]
        private static string GetFirstLine(object data)
        {
            switch (data)
            {
                case null:
                case DBNull _: return default;
                case string value:
                    return
                        string.IsNullOrEmpty(value)
                            ? default
                            : value.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                default: throw new ArgumentException($"Invalid data type. Expected {typeof(string).Name} but found {data.GetType().Name}.");
            }
        }
    }

    public class ExtractFirstLineColumn
    {
        /// <summary>
        /// Gets or sets the data-table column containing json.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string From { get; set; }

        /// <summary>
        /// Gets or sets the name of the column to attach.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string Attach { get; set; }
    }
}