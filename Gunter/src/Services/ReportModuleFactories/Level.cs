using Gunter.Data;
using Gunter.Workflows;

namespace Gunter.Reporting.Modules
{
    public class Level : RenderDto<>
    {
        public override IReportModule Create(TestContext context)
        {
            var level = context.TestCase.Level.ToString();

            return new ReportModule<Level>
            {
                Text = level,
                Ordinal = Ordinal
            };
        }
    }
}