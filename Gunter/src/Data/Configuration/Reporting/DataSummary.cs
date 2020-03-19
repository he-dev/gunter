using System.Collections.Generic;
using Gunter.Data.Configuration.Abstractions;
using Gunter.Data.Configuration.Reporting.Abstractions;

namespace Gunter.Data.Configuration.Reporting
{
    public class DataSummary : ReportModule, ITabular
    {
        public TableOrientation Orientation => TableOrientation.Horizontal;

        public bool HasFoot => true;

        public List<DataColumnSetting?> Columns { get; set; } = new List<DataColumnSetting?>();
    }
}