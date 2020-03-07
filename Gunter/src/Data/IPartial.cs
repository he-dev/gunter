using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;

namespace Gunter.Data
{
    public interface IPartial
    {
        [JsonRequired]
        SoftString Id { get; set; }
        
        Merge? Merge { get; set; }
    }
}