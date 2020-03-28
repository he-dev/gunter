using Gunter.Annotations;
using Newtonsoft.Json;

namespace Gunter.Data.Abstractions
{
    [Gunter]
    public interface IModel
    {
        [JsonRequired]
        string Name { get; set; }
    }

    public interface IMergeable
    {
        [JsonProperty("Merge")]
        ModelSelector? ModelSelector { get; set; }
    }
}