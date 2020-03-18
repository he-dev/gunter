using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Gunter.Annotations;
using Gunter.Extensions;
using Gunter.Services.Abstractions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gunter.Services.DataFilters
{
    [Gunter]
    [UsedImplicitly]
    public class GetJsonValue : IFilterData
    {
        [JsonProperty(Required = Required.Always)]
        public IList<JsonColumnSetting>? Columns { get; set; }

        public void Execute(DataTable dataTable)
        {
            //if (Columns is null) throw new InvalidOperationException($"{nameof(GetJsonValue)}Filter requires at least one column.");
            
            var tokens =
                from dataRow in dataTable.InitializeTargetColumns(Columns).AsEnumerable()
                from column in Columns
                let value = dataRow.Field<object>(column.From) as string
                let token = value is {} && value.IsJson() ? JToken.Parse(value).SelectToken(column.Select) : column.Default
                select (dataRow, column, token);

            foreach (var (dataRow, column, token) in tokens)
            {
                dataRow[column.Into] = token;
            }
        }
    }

    [PublicAPI]
    [UsedImplicitly]
    public class JsonColumnSetting : DataColumnSetting
    {
        /// <summary>
        /// Gets or sets JsonPath for extracting the value.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Select { get; set; }

        /// <summary>
        /// Gets or sets the default value to use when nothing found.
        /// </summary>
        public object Default { get; set; }
    }
}