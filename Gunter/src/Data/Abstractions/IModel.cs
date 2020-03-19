using Gunter.Annotations;
using Newtonsoft.Json;

namespace Gunter.Data.Abstractions
{
    [Gunter]
    public interface IModel
    {
        string? Name { get; set; }
    }

    public interface IMergeable
    {
        [JsonProperty("Merge")]
        ModelSelector? ModelSelector { get; set; }
    }
}