using System.ComponentModel;
using Gunter.Reporting;

namespace Gunter.Data.Configuration.Reporting
{
    public class TestInfo : ReportModule, ITabular
    {
        public TableOrientation Orientation => TableOrientation.Vertical;

        public bool HasFoot => false;

        //[DefaultValue(@"dd\.hh\:mm\:ss")]
        [DefaultValue(@"mm\:ss\.fff")]
        public string TimespanFormat { get; set; }
    }
}