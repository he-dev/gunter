using System.ComponentModel;
using Gunter.Data.Configuration.Reports.CustomSections.Abstractions;

namespace Gunter.Data.Configuration.Reports.CustomSections
{
    public class TestSummary : CustomSection, ITabular
    {
        public TableOrientation Orientation => TableOrientation.Vertical;

        public bool HasFoot => false;

        //[DefaultValue(@"dd\.hh\:mm\:ss")]
        [DefaultValue(@"mm\:ss\.fff")]
        public string TimespanFormat { get; set; }
    }
}