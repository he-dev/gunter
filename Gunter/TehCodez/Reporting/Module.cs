using System.Collections.Generic;
using System.Data;
using Gunter.Data;
using Gunter.Services;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Gunter.Reporting
{
    [PublicAPI]
    public interface IModule : IResolvable
    {
        [CanBeNull]
        string Heading { get; set; }

        [CanBeNull]
        string Text { get; set; }
    }

    public abstract class Module : IModule
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
    }    

    public interface ITabular
    {
        [JsonIgnore]
        TableOrientation Orientation { get; }

        [JsonIgnore]
        bool HasFooter { get; }

        DataTable Create(TestUnit testUnit);
    }    

    public enum TableOrientation
    {
        // Header on top
        Horizontal,

        // Header on the left
        Vertical
    }
}
