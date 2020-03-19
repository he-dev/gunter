using System.Collections.Generic;
using System.Data;
using System.Linq;
using Gunter.Annotations;
using Gunter.Extensions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gunter.Services.FilterData
{
    using Gunter.Services.Abstractions;

    [Gunter]
    [UsedImplicitly]
    public class GetJsonValue : FilterDataBase
    {
        /// <summary>
        /// Gets or sets JsonPath for extracting the value.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Select { get; set; }

        /// <summary>
        /// Gets or sets the default value to use when nothing found.
        /// </summary>
        public object? Default { get; set; }

        public override void Execute(DataTable dataTable, DataRow currentRow)
        {
            dataTable.InitializeColumn(Into);

            if (currentRow.Field<object>(From) is string value)
            {
                currentRow[Into] = value is {} && value.IsJson() ? JToken.Parse(value).SelectToken(Select) : Default;
            }
        }
    }
}