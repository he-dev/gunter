using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;

namespace Gunter.Data
{
    public interface IMergeable
    {
        [JsonRequired]
        SoftString Id { get; set; }
        
        [CanBeNull]
        Merge Merge { get; set; }
    }
}