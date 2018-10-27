using Gunter.Data;
using Gunter.Data.Dtos;
using Gunter.Services;

namespace Gunter.Reporting.Modules
{
    public class Level : Module
    {
        public override SectionDto CreateDto(TestContext context)
        {
            var format = (FormatFunc)context.Formatter.Format;

            var level = context.TestCase.Level.ToString();

            return new SectionDto
            {
                Text = level,
                Ordinal = Ordinal
            };            
        }
    }
}