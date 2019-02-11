using Gunter.Data;
using Gunter.Data.Dtos;
using Gunter.Services;

namespace Gunter.Reporting.Modules
{
    public class Greeting : Module
    {
        public override ModuleDto CreateDto(TestContext context)
        {
            var format = (FormatFunc)context.Formatter.Format;

            return new ModuleDto
            {
                Heading = format(Heading),
                Text = format(Text),
                Ordinal = Ordinal
            };
        }
    }
}