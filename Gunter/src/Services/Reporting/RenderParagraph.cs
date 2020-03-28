using Gunter.Data.Configuration.Reports.CustomSections;
using Gunter.Services.Abstractions;
using Reusable.Extensions;

namespace Gunter.Services.Reporting
{
    public class RenderParagraph<T> : IRenderReportSection<T> where T : Paragraph
    {
        public RenderParagraph(ITryGetFormatValue tryGetFormatValue)
        {
            TryGetFormatValue = tryGetFormatValue;
        }

        private ITryGetFormatValue TryGetFormatValue { get; }

        public IReportSectionDto Execute(T section)
        {
            return ReportSectionDto.Create(section, paragraph => new
            {
                text = paragraph.Text.Format(TryGetFormatValue),
            });
        }
    }
}