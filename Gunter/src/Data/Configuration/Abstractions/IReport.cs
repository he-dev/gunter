using System.Collections.Generic;
using Gunter.Data.Abstractions;
using Gunter.Data.Configuration.Reports.CustomSections.Abstractions;

namespace Gunter.Data.Configuration.Abstractions
{
    public interface IReport : IModel, IMergeable
    {
        string Title { get; }

        List<CustomSection> Modules { get; }
    }
}