using System.ComponentModel;
using Gunter.Data.Configuration.Reports.CustomSections.Abstractions;
using Newtonsoft.Json;

namespace Gunter.Data.Configuration.Reports.CustomSections
{
    public class QuerySummary : CustomSection, ITabular
    {
        [DefaultValue("Timestamp")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string TimestampColumn { get; set; } = default!;

        // Custom TimeSpan Format Strings https://msdn.microsoft.com/en-us/library/ee372287(v=vs.110).aspx

        [DefaultValue(@"mm\:ss\.fff")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string TimespanFormat { get; set; } = default!;

        public TableOrientation Orientation => TableOrientation.Vertical;

        public bool HasFoot => false;
    }
}