using System;
using System.Collections.Generic;
using System.Data;
using Gunter.Annotations;
using Gunter.Extensions;
using Gunter.Services.Abstractions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reusable.Exceptionize;

namespace Gunter.Services.DataFilters
{
    [Gunter]
    [UsedImplicitly]
    public class GetJsonValue : IDataFilter
    {
        [JsonProperty(Required = Required.Always)]
        public IList<GetJsonValueColumn>? Columns { get; set; }

        public void Execute(DataTable dataTable)
        {
            if (Columns is null) throw new InvalidOperationException($"{nameof(GetJsonValue)}Filter requires at least one column.");

            foreach (var column in Columns)
            {
                if (dataTable.Columns.Contains(column.Attach))
                {
                    throw DynamicException.Create("ColumnAlreadyExists", $"The data-table already contains a column with the name '{column.Attach}'.");
                }

                dataTable.Columns.Add(new DataColumn(column.Attach, typeof(object)));
                
                foreach (var dataRow in dataTable.AsEnumerable())
                {
                    try
                    {
                        var value = GetJsonValueInternal(dataRow, column.From, column.Select, column.Default);
                        dataRow[column.Attach] = value;
                    }
                    catch (Exception inner)
                    {
                        throw DynamicException.Create("DataPostProcessor", $"Could not attach column '{column.Attach}'.", inner);
                    }
                }
            }
        }

        private static object GetJsonValueInternal(DataRow source, string column, string path, object defaultValue)
        {
            var value = source.Field<string>(column);

            if (string.IsNullOrWhiteSpace(value) || !value.IsJson())
            {
                return default;
            }

            var jToken = JToken.Parse(value).SelectToken(path);
            return jToken switch
            {
                null => defaultValue,
                JValue jValue => jValue.Value,
                _ => defaultValue
            };
        }
    }

    [PublicAPI]
    [UsedImplicitly]
    public class GetJsonValueColumn
    {
        /// <summary>
        /// Gets or sets the data-table column containing json.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string From { get; set; }

        /// <summary>
        /// Gets or sets JsonPath for extracting the value.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Select { get; set; }

        /// <summary>
        /// Gets or sets the name of the column to attach.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Attach { get; set; }

        /// <summary>
        /// Gets or sets the default value to use when nothing found.
        /// </summary>
        public object Default { get; set; }
    }
}