using System;
using System.Collections.Generic;
using Gunter.Data;
using Gunter.Data.Configuration.Reports.CustomSections.Abstractions;
using Gunter.Data.ReportSections;

namespace Gunter.Services.Abstractions
{
    public interface IRenderReportSection<in T> where T : CustomSection
    {
        ReportSectionDto Execute(T model);
    }
}