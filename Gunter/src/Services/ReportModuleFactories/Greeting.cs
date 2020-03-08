using Gunter.Data;
using Reusable.Extensions;

namespace Gunter.Reporting.Modules
{
    public class Greeting : ReportModuleFactory
    {
        public override IReportModule Create(TestContext context)
        {
            return new ReportModule<Greeting>
            {
                Heading = Heading.Format(context.RuntimeProperties),
                Text = Text.Format(context.RuntimeProperties),
                Ordinal = Ordinal
            };
        }
    }
}