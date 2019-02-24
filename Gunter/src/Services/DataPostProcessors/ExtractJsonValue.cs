using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using Gunter.Data;
using Gunter.Extensions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reusable.Exceptionizer;

namespace Gunter.Services.DataPostProcessors
{
    [UsedImplicitly]
    public class ExtractJsonValue : IDataPostProcessor
    {
        [NotNull, ItemNotNull]
        [JsonProperty(Required = Required.Always)]
        public IList<ExtractJsonValueColumn> Columns { get; set; }

        public void Execute(DataTable dataTable)
        {
            if (Columns is null) throw new InvalidOperationException($"There are no '{nameof(Columns)}'.");
            
            foreach (var column in Columns)
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
                        var value = GetJsonValue(dataRow, column.From, column.Select, column.Default);
                        dataRow[column.Attach] = value;
                    }
                    catch (Exception inner)
                    {
                        throw DynamicException.Create("AttachmentCompute", $"Could not compute the '{column.Attach}' attachment.", inner);
                    }
                }
            }
        }

        private static object GetJsonValue(DataRow source, string column, string path, object defaultValue)
        {
            var value = source.Field<string>(column);

            if (string.IsNullOrWhiteSpace(value) || !value.IsJson())
            {
                return default;
            }

            var jToken = JToken.Parse(value).SelectToken(path);
            switch (jToken)
            {
                case null: return defaultValue;
                case JValue jValue: return jValue.Value;
                default: return defaultValue;
            }
        }
    }

    public class ExtractJsonValueColumn
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