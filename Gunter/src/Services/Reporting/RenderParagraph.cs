using Gunter.Data.Configuration.Reports.CustomSections;
using Gunter.Data.Configuration.Reports.CustomSections.Abstractions;
using Gunter.Services.Abstractions;
using Reusable.Extensions;

namespace Gunter.Services.Reporting
{
    public class RenderParagraph : IRenderReportModule
    {
        public RenderParagraph(ITryGetFormatValue tryGetFormatValue)
        {
            TryGetFormatValue = tryGetFormatValue;
        }

        private ITryGetFormatValue TryGetFormatValue { get; }

        public IReportModuleDto Execute(CustomSection section)
        {
            return new ReportModuleDto<Paragraph>(section, heading => new
            {
                text = heading.Text.Format(TryGetFormatValue),
            });
        }
    }
}