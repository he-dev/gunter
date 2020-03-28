using Gunter.Data.Configuration.Reports.CustomSections.Abstractions;
using Gunter.Data.Configuration.Sections;

namespace Gunter.Data.Configuration.Reports.CustomSections
{
    public class Paragraph : CustomSection
    {
        public string Text { get; set; }
    }

    public class Level : Paragraph
    {
        public Level()
        {
            Text = $"{{{nameof(TestCase)}.{nameof(TestCase.Level)}}}";
            Tags.Add("level");
        }
    }
    
    public class Signature : Paragraph
    {
        public Signature()
        {
            Text = $"{{{nameof(ProgramInfo)}.{nameof(ProgramInfo.FullName)}}}";
            Tags.Add("signature");
        }
    }
}