using System.Data;
using Newtonsoft.Json;

namespace Gunter.Services.Abstractions
{
    public interface IFilterData
    {
        void Execute(DataTable data, DataRow currentRow);
    }
    
    public abstract class FilterDataBase : IFilterData
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
        
        public abstract void Execute(DataTable data, DataRow currentRow);
    }
}