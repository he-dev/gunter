using System;
using System.Collections.Generic;
using Gunter.Data.Configuration.Reports.CustomSections.Abstractions;

namespace Gunter.Services.Abstractions
{
    public interface IRenderReportModule
    {
        IReportModuleDto Execute(CustomSection section);
    }

    public interface IReportModuleDto
    {
        string Name { get; }
        
        HashSet<string> Tags { get; }
        
        object Body { get; }
    }

    public class ReportModuleDto<T> : IReportModuleDto where T : CustomSection
    {
        public ReportModuleDto(CustomSection model, Func<T, object> createBody)
        {
            Name = model.Name;
            Tags = model.Tags;
            Body = createBody(model as T);
        }

        public string Name { get; }

        public HashSet<string> Tags { get; }

        public object Body { get; }
    }
}