using Gunter.Data.Configuration;
using Gunter.Workflows;

namespace Gunter.Data.Reporting
{
    public class Greeting : RenderDto<>
    {
        public override IReportModule Create(TestContext context)
        {
            return new ReportModule<Greeting>
            {
                Heading = Heading.Format(context.Container),
                Text = Text.Format(context.Container),
                Ordinal = Ordinal
            };
        }
    }
}