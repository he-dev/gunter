using Gunter.Data.Configuration;
using Gunter.Data.Configuration.Reporting;
using Gunter.Services.Abstractions;
using Reusable.Extensions;

namespace Gunter.Services.Reporting
{
    public class RenderHeading : IRenderReportModule
    {
        public RenderHeading(Format format)
        {
            Format = format;
        }

        private Format Format { get; }

        public IReportModuleDto Execute(ReportModule module)
        {
            return new ReportModuleDto<Heading>(module, heading => new
            {
                text = heading.Text.Map(Format),
                level = heading.Level
            });
        }
    }
}