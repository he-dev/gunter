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
    public class FirstLine : IDataPostProcessor
    {
        [NotNull, ItemNotNull]
        [JsonProperty(Required = Required.Always)]
        public IList<FirstLineColumn> Columns { get; set; }

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
            if (data is null || data is DBNull)
            {
                return default;
            }

            if (!(data is string value))
            {
                throw new ArgumentException($"Invalid data type. Expected {typeof(string).Name} but found {data.GetType().Name}.");
            }

            return
                string.IsNullOrEmpty(value)
                    ? string.Empty
                    : value.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }
    }

    public class FirstLineColumn
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