using Gunter.Data;
using Gunter.Data.Dtos;
using Gunter.Services;
using Reusable.Extensions;

namespace Gunter.Reporting.Modules
{
    public class Greeting : Module
    {
        public override ModuleDto CreateDto(TestContext context)
        {
            return new ModuleDto
            {
                Heading = Heading.Format(context.RuntimeVariables),
                Text = Text.Format(context.RuntimeVariables),
                Ordinal = Ordinal
            };
        }
    }
}