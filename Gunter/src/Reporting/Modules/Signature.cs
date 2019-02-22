using Gunter.Data;
using Gunter.Data.Dtos;
using Gunter.Services;
using Reusable.Extensions;

namespace Gunter.Reporting.Modules
{
    public class Signature : Module
    {
        public override ModuleDto CreateDto(TestContext context)
        {
            return new ModuleDto
            {
                Text = $"{RuntimeValue.Program.FullName}".Format(context.RuntimeVariables),
                Ordinal = Ordinal
            };
        }
    }
}