using Gunter.Data;
using Gunter.Data.Dtos;
using Gunter.Services;

namespace Gunter.Reporting.Modules
{
    public class Signature : Module
    {
        public override SectionDto CreateDto(TestContext context)
        {
            var format = (FormatFunc)context.Formatter.Format;

            return new SectionDto
            {
                Text = format($"{RuntimeVariable.Program.FullName}"),
                Ordinal = Ordinal
            };
        }
    }
}