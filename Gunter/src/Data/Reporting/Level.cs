using Gunter.Data.Configuration;
using Gunter.Workflows;

namespace Gunter.Data.Reporting
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