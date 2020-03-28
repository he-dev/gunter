using Newtonsoft.Json;

namespace Gunter.Data.Configuration.Reports.CustomSections.Abstractions
{
    public interface ITabular
    {
        [JsonIgnore]
        TableOrientation Orientation { get; }

        [JsonIgnore]
        bool HasFoot { get; }
    }
}