using Gunter.Annotations;
using Newtonsoft.Json;

namespace Gunter.Data.Abstractions
{
    [Gunter]
    public interface IModel
    {
        [JsonRequired]
        string Name { get; }
    }

    public interface IMergeable : IModel
    {
        [JsonProperty("Merge")]
        ModelSelector? ModelSelector { get; set; }
    }
}