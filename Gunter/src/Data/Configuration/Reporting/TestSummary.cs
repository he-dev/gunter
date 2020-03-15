using System.ComponentModel;
using Gunter.Data.Configuration.Reporting.Abstractions;

namespace Gunter.Data.Configuration.Reporting
{
    public class TestSummary : ReportModule, ITabular
    {
        public TableOrientation Orientation => TableOrientation.Vertical;

        public bool HasFoot => false;

        //[DefaultValue(@"dd\.hh\:mm\:ss")]
        [DefaultValue(@"mm\:ss\.fff")]
        public string TimespanFormat { get; set; }
    }
}