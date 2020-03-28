using Gunter.Data.Configuration.Reports.CustomSections;
using Gunter.Services.Abstractions;
using Reusable.Extensions;

namespace Gunter.Services.Reporting
{
    public class RenderHeading : IRenderReportSection<Heading>
    {
        public RenderHeading(ITryGetFormatValue tryGetFormatValue)
        {
            TryGetFormatValue = tryGetFormatValue;
            TryGetFormatValue = tryGetFormatValue;
        }

        private ITryGetFormatValue TryGetFormatValue { get; }

        public IReportSectionDto Execute(Heading section)
        {
            return ReportSectionDto.Create(section, heading => new
            {
                text = heading.Text.Format(TryGetFormatValue),
                level = heading.Level
            });
        }
    }
}