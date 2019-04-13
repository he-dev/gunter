using Gunter.Data;
using Gunter.Services;
using Reusable.Extensions;

namespace Gunter.Reporting.Modules
{
    public class Greeting : Module
    {
        public override IModuleDto CreateDto(TestContext context)
        {
            return new ModuleDto<Greeting>
            {
                Heading = Heading.Format(context.RuntimeVariables),
                Text = Text.Format(context.RuntimeVariables),
                Ordinal = Ordinal
            };
        }
    }
}