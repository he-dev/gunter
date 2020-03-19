using System.ComponentModel;
using Gunter.Data.Configuration.Abstractions;
using Gunter.Data.Configuration.Reporting.Abstractions;

namespace Gunter.Data.Configuration.Reporting
{
    public class QuerySummary : ReportModule, ITabular
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