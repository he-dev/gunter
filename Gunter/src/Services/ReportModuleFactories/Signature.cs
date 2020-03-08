using Gunter.Data;
using Reusable.Extensions;

namespace Gunter.Reporting.Modules
{
    public class Signature : ReportModuleFactory
    {
        public override IReportModule Create(TestContext context)
        {
            return new ReportModule<Signature>
            {
                Text = $"{RuntimeProperty.BuiltIn.Program.FullName}".Format(context.RuntimeProperties),
                Ordinal = Ordinal
            };
        }
    }
}