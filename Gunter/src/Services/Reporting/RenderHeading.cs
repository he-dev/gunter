using Gunter.Data;
using Gunter.Data.Configuration.Reports.CustomSections;
using Gunter.Data.ReportSections;
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

        public ReportSectionDto Execute(Heading section)
        {
            return new HeadingDto(section)
            {
                Text = section.Text.Format(TryGetFormatValue),
                Level = section.Level,
                Tags =
                {
                    "section-heading"
                }
            };
        }
    }
}