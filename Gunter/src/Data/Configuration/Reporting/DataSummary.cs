using System.Collections.Generic;
using Gunter.Reporting;
using Gunter.Services.Reporting;

namespace Gunter.Data.Configuration.Reporting
{
    [Service(typeof(RenderDataSummary))]
    public class DataSummary : ReportModule, ITabular
    {
        public TableOrientation Orientation => TableOrientation.Horizontal;

        public bool HasFoot => true;

        public List<DataInfoColumn?> Columns { get; set; } = new List<DataInfoColumn?>();
    }
}