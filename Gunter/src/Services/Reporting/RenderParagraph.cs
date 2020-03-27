using Gunter.Data.Configuration.Abstractions;
using Gunter.Data.Configuration.Reporting;
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

        public IReportModuleDto Execute(ReportModule module)
        {
            return new ReportModuleDto<Paragraph>(module, heading => new
            {
                text = heading.Text.Format(TryGetFormatValue),
            });
        }
    }
}