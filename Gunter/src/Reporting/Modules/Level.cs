using Gunter.Data;
using Gunter.Data.Dtos;
using Gunter.Services;

namespace Gunter.Reporting.Modules
{
    public class Level : Module
    {
        public override ModuleDto CreateDto(TestContext context)
        {
            var level = context.TestCase.Level.ToString();

            return new ModuleDto
            {
                Text = level,
                Ordinal = Ordinal
            };
        }
    }
}