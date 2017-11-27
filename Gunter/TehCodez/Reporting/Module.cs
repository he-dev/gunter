using System.Collections.Generic;
using System.Data;
using Gunter.Data;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Gunter.Reporting
{
    [PublicAPI]
    public interface IModule
    {
        [CanBeNull]
        string Heading { get; set; }

        [CanBeNull]
        string Text { get; set; }
    }

    public abstract class Module : IModule
    {
        public string Heading { get; set; }

        public string Text { get; set; }
    }

    public interface ITabular
    {
        [JsonIgnore]
        TableOrientation Orientation { get; }

        [JsonIgnore]
        bool HasFooter { get; }

        DataTable Create(TestContext context);
    }

    public enum TableOrientation
    {
        // Header on top
        Horizontal,

        // Header on the left
        Vertical
    }

    public interface IDataFilter
    {
        object Apply(object data);
    }

    [UsedImplicitly, PublicAPI]
    public interface IFormatter
    {
        string Apply(object value);
    }
}
