using System;
using Newtonsoft.Json;

namespace Gunter.Data.Configuration.Reporting.Abstractions
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