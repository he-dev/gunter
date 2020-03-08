using Gunter.Data;

namespace Gunter.Reporting.Modules
{
    public class Level : ReportModuleFactory
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