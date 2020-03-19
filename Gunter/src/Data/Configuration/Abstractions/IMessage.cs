using Gunter.Data.Abstractions;
using Newtonsoft.Json;

namespace Gunter.Data.Configuration.Abstractions
{
    public interface IMessage : IModel
    {
        [JsonProperty("Report")]
        string? ReportName { get; set; }
    }
}