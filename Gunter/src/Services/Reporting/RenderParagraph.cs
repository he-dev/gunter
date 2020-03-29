using Gunter.Data;
using Gunter.Data.Configuration.Reports.CustomSections;
using Gunter.Data.ReportSections;
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

        public ReportSectionDto Execute(T section)
        {
            return new ParagraphDto(section)
            {
                Text = section.Text.Format(TryGetFormatValue),
                Tags =
                {
                    "section-paragraph"
                }
            };
        }
    }
}