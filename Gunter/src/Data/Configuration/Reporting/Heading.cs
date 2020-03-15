using System.Net.Mime;
using Gunter.Workflows;

namespace Gunter.Data.Configuration.Reporting
{
    public class Heading : ReportModule
    {
        public string Text { get; set; }

        public int Level { get; set; } = 1;
    }
}