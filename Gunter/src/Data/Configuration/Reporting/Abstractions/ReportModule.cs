using Newtonsoft.Json;

namespace Gunter.Data.Configuration.Reporting.Abstractions
{
    public interface ITabular
    {
        [JsonIgnore]
        TableOrientation Orientation { get; }

        [JsonIgnore]
        bool HasFoot { get; }
    }

    public enum TableOrientation
    {
        // Header on top
        Horizontal,

        // Header on the left
        Vertical
    }
}