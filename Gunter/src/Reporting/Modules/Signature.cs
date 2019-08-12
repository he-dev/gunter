using Gunter.Data;
using Gunter.Services;
using Reusable.Extensions;

namespace Gunter.Reporting.Modules
{
    public class Signature : Module
    {
        public override IModuleDto CreateDto(TestContext context)
        {
            return new ModuleDto<Signature>
            {
                Text = $"{RuntimeProperty.BuiltIn.Program.FullName}".Format(context.RuntimeProperties),
                Ordinal = Ordinal
            };
        }
    }
}