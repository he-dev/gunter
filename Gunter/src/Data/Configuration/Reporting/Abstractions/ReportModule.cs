using System;
using Gunter.Annotations;
using Gunter.Data;
using Gunter.Workflows;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Gunter.Reporting
{
    public class ServiceAttribute : Attribute
    {
        public ServiceAttribute(Type serviceType) => ServiceType = serviceType;

        public Type ServiceType { get; }

        public static implicit operator Type(ServiceAttribute serviceAttribute) => serviceAttribute.ServiceType;
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