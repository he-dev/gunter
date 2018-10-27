using Gunter.Data;
using Gunter.Data.Dtos;
using Gunter.Services;

namespace Gunter.Reporting.Modules
{
    public class Greeting : Module
    {
        public override SectionDto CreateDto(TestContext context)
        {
            var format = (FormatFunc)context.Formatter.Format;

            return new SectionDto
            {
                Heading = format(Heading),
                Text = format(Text),
                Ordinal = Ordinal
            };
        }
    }
}