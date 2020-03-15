using System.Collections.Generic;
using Gunter.Reporting;

namespace Gunter.Data.Configuration.Abstractions
{
    public interface IReport : IModel, IMergeable
    {
        string Title { get; }

        List<ReportModule> Modules { get; }
    }
}