using Gunter.Data;

namespace Gunter.Reporting.Modules
{
    public class Level : Module
    {
        public override IModuleDto CreateDto(TestContext context)
        {
            var level = context.TestCase.Level.ToString();

            return new ModuleDto<Level>
            {
                Text = level,
                Ordinal = Ordinal
            };
        }
    }
}