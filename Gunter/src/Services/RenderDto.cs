using System;
using Gunter.Annotations;
using Gunter.Data;
using Gunter.Workflows;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Gunter.Reporting
{
    [PublicAPI]
    public interface IReportModuleFactory
    {
        string? Heading { get; set; }

        string? Text { get; set; }

        int Ordinal { get; set; }

        IReportModule Create(TestContext context);
    }

    [Gunter]
    public abstract class ReportModule
    {
        public string? Heading { get; set; }

        public string? Text { get; set; }

        public int Ordinal { get; set; }
    }

    public class RendererAttribute : Attribute
    {
        public RendererAttribute(Type rendererType) => RendererType = rendererType;

        public Type RendererType { get; }

        public static implicit operator Type(RendererAttribute rendererAttribute) => rendererAttribute.RendererType;
    }

    public interface IRenderDto
    {
        IReportModule Execute(ReportModule model);
    }

    public interface ITabular
    {
        [JsonIgnore]
        TableOrientation Orientation { get; }

        [JsonIgnore]
        bool HasFoot { get; }
    }

    public enum TableOrientation
    {
        // Header on top
        Horizontal,

        // Header on the left
        Vertical
    }
}