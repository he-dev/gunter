using Gunter.Data.Configuration;
using Gunter.Workflows;

namespace Gunter.Data.Reporting
{
    public class Signature : RenderDto<>
    {
        public override IReportModule Create(TestContext context)
        {
            return new ReportModule<Signature>
            {
                Text = $"{RuntimeProperty.BuiltIn.Program.FullName}".Format(context.Container),
                Ordinal = Ordinal
            };
        }
    }
}