using System.Collections.Generic;
using System.Data;
using Gunter.Data;
using Gunter.Services;
using Newtonsoft.Json;

namespace Gunter.Reporting
{
    public interface ISection : IResolvable
    {
        string Heading { get; set; }

        string Text { get; set; }

        ISectionDetail Detail { get; set; }
    }

    public class Section : ISection
    {
        private string _text;
        private string _heading;

        [JsonIgnore]
        public IVariableResolver Variables { get; set; } = VariableResolver.Empty;

        public string Heading
        {
            get => Variables.Resolve(_heading);
            set => _heading = value;
        }

        public string Text
        {
            get => Variables.Resolve(_text);
            set => _text = value;
        }

        public ISectionDetail Detail { get; set; }
    }

    public interface ISectionDetail
    {
        [JsonIgnore]
        TableOrientation Orientation { get; }

        DataSet Create(TestUnit testUnit);
    }

    public enum TableOrientation
    {
        Horizontal,
        Vertical
    }
}
