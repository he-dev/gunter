using System.Collections.Generic;
using Gunter.Data.Configuration.Reporting;
using Gunter.Data.Configuration.Reports.CustomSections.Abstractions;

namespace Gunter.Data.Configuration.Reports.CustomSections
{
    public class DataSummary : CustomSection, ITabular
    {
        public TableOrientation Orientation => TableOrientation.Horizontal;

        public bool HasFoot => true;

        public List<DataColumnSetting?> Columns { get; set; } = new List<DataColumnSetting?>();
    }
}