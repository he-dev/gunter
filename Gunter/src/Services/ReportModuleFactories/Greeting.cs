using Gunter.Data;
using Gunter.Workflows;
using Reusable.Extensions;

namespace Gunter.Reporting.Modules
{
    public class Greeting : ReportModuleFactory
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