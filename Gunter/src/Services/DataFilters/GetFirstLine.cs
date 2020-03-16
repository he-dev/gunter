using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Gunter.Annotations;
using Gunter.Services.Abstractions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable.Exceptionize;
using Reusable.Extensions;

namespace Gunter.Services.DataFilters
{
    [Gunter]
    public class GetFirstLine : IFilterData
    {
        [JsonProperty(Required = Required.Always)]
        public List<GetFirstLineColumn>? Columns { get; set; }

        public void Execute(DataTable dataTable)
        {
            if (Columns is null) throw new InvalidOperationException($"{nameof(GetFirstLine)}Filter requires at least one column.");

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
                        var firstLine = GetFirstLineInternal(dataRow.Field<object>(column.From));
                        dataRow[column.Attach ?? column.From] = firstLine;
                    }
                    catch (Exception inner)
                    {
                        throw DynamicException.Create("AttachmentCompute", $"Could not compute the '{column.Attach ?? column.From}' attachment.", inner);
                    }
                }
            }
        }

        private static string? GetFirstLineInternal(object data)
        {
            return data switch
            {
                null => default,
                DBNull _ => default,
                string value => (string.IsNullOrEmpty(value) ? default : value.Split(new[] { "\r\n", "\r" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()),
                _ => throw new ArgumentException($"Invalid data type. Expected {typeof(string).Name} but found {data.GetType().Name}.")
            };
        }
    }

    public class GetFirstLineColumn
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