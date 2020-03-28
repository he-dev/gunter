using System.ComponentModel;
using Gunter.Data.Configuration.Reports.CustomSections.Abstractions;

namespace Gunter.Data.Configuration.Reports.CustomSections
{
    public class QuerySummary : CustomSection, ITabular
    {
        [DefaultValue("Timestamp")]
        public string TimestampColumn { get; set; }

        // Custom TimeSpan Format Strings https://msdn.microsoft.com/en-us/library/ee372287(v=vs.110).aspx

        [DefaultValue(@"mm\:ss\.fff")]
        public string TimespanFormat { get; set; }

        public TableOrientation Orientation => TableOrientation.Vertical;

        public bool HasFoot => false;
    }
}