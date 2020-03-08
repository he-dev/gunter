using Newtonsoft.Json;
using Reusable.Utilities.Mailr.Models;

namespace Gunter.Reporting
{
    public interface IReportModule
    {
        string Name { get; }
        
        int Ordinal { get; }
        
        string Heading { get; }
        
        string Text { get; }
        
        HtmlTable Data { get; }
    }

    public class ReportModule<T> : IReportModule
    {
        [JsonProperty("$t")]
        public string Name => typeof(T).Name;

        public int Ordinal { get; set; }

        public string Heading { get; set; }

        public string Text { get; set; }

        public HtmlTable Data { get; set; }
    }
}