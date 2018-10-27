using Newtonsoft.Json;
using Reusable;

namespace Gunter.Data
{
    public interface IMergeable
    {
        [JsonRequired]
        SoftString Id { get; set; }
        
        Merge Merge { get; set; }
    }
}