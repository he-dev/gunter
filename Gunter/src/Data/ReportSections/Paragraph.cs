using Gunter.Data.Configuration.Reports.CustomSections.Abstractions;
using Reusable.Utilities.Mailr.Models;

namespace Gunter.Data.ReportSections
{
    public class ParagraphDto : ReportSectionDto
    {
        public ParagraphDto(CustomSection from) : base(from) { }

        public string Text { get; set; }
    }

    public class HeadingDto : ReportSectionDto
    {
        public HeadingDto(CustomSection section) : base(section) { }

        public string Text { get; set; }

        public int Level { get; set; } = 1;
    }

    public class TableDto : ReportSectionDto
    {
        public TableDto(CustomSection section) : base(section) { }

        public HtmlTable Data { get; set; }
    }
}