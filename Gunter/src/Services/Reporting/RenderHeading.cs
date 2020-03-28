using Gunter.Data.Configuration.Reports.CustomSections;
using Gunter.Data.Configuration.Reports.CustomSections.Abstractions;
using Gunter.Services.Abstractions;
using Reusable.Extensions;

namespace Gunter.Services.Reporting
{
    public class RenderHeading : IRenderReportModule
    {
        public RenderHeading(ITryGetFormatValue tryGetFormatValue)
        {
            TryGetFormatValue = tryGetFormatValue;
            TryGetFormatValue = tryGetFormatValue;
        }

        private ITryGetFormatValue TryGetFormatValue { get; }

        public IReportModuleDto Execute(CustomSection section)
        {
            return new ReportModuleDto<Heading>(section, heading => new
            {
                text = heading.Text.Format(TryGetFormatValue),
                level = heading.Level
            });
        }
    }
}