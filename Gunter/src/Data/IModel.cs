using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;

namespace Gunter.Data
{
    public interface IModel
    {
        [JsonRequired]
        SoftString Id { get; }

        Merge? Merge { get; }
    }
}