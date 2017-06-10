using System.Collections.Generic;
using System.Data;
using Gunter.Data;
using Newtonsoft.Json;

namespace Gunter.Reporting
{
    public interface ISection
    {
        string Heading { get; set; }

        string Text { get; set; }

        ISectionDetail Detail { get; set; }
    }

    public class Section : ISection
    {
        public string Heading { get; set; }

        public string Text { get; set; }

        public ISectionDetail Detail { get; set; }
    }

    public interface ISectionDetail
    {
        [JsonIgnore]
        TableOrientation Orientation { get; }

        DataSet Create(TestContext context);
    }

    public enum TableOrientation
    {
        Horizontal,
        Vertical
    }
}
