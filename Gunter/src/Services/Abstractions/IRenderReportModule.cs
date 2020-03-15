using System;
using System.Collections.Generic;
using Gunter.Data.Configuration;

namespace Gunter.Services.Abstractions
{
    public interface IRenderReportModule
    {
        IReportModuleDto Execute(ReportModule module);
    }

    public interface IReportModuleDto
    {
        string Name { get; }
        
        HashSet<string> Tags { get; }
        
        object Body { get; }
    }

    public class ReportModuleDto<T> : IReportModuleDto where T : ReportModule
    {
        public ReportModuleDto(ReportModule model, Func<T, object> createBody)
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