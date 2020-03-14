using Newtonsoft.Json;
using Reusable.Extensions;
using Reusable.Utilities.Mailr.Models;

namespace Gunter.Data.Configuration
{
    public interface IReportModule
    {
        string Name { get; }
    }

    public abstract class ReportModule : IReportModule
    {
        public string Name => GetType().ToPrettyString();
    }

    public class Heading : ReportModule
    {
        public string Text { get; set; }

        public int Level { get; set; }
    }
    
    public class Paragraph : ReportModule
    {
        public string Text { get; set; }
    }

    public class ReportModule<T> : IReportModule
    {
        //[JsonProperty("$t")]
        public string Name => typeof(T).Name;

        public int Ordinal { get; set; }

        public string Heading { get; set; }

        public string Text { get; set; }

        public HtmlTable Data { get; set; }
    }
}