using System.Collections.Generic;
using System.Data;
using Gunter.Data;
using Gunter.Data.Dtos;
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

        int Ordinal { get; set; }

        ModuleDto CreateDto(TestContext context);
    }

    public abstract class Module : IModule
    {
        public string Heading { get; set; }

        public string Text { get; set; }

        public int Ordinal { get; set; }

        public abstract ModuleDto CreateDto(TestContext context);
    }

    public interface ITabular
    {
        [JsonIgnore]
        TableOrientation Orientation { get; }

        [JsonIgnore]
        bool HasFoot { get; }

        //DataTable Create(TestContext context);
    }

    public enum TableOrientation
    {
        // Header on top
        Horizontal,

        // Header on the left
        Vertical
    }
}