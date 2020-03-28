using Gunter.Data.Configuration.Reports.CustomSections.Abstractions;

namespace Gunter.Data.Configuration.Reports.CustomSections
{
    public class Heading : CustomSection
    {
        public string Text { get; set; } = default!;

        public int Level { get; set; } = 1;
    }
}