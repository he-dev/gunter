using System;
using System.Collections.Generic;
using Gunter.Data.Configuration.Reports.CustomSections.Abstractions;

namespace Gunter.Services.Abstractions
{
    public interface IRenderReportSection<in T> where T : CustomSection
    {
        IReportSectionDto Execute(T model);
    }

    public interface IReportSectionDto
    {
        string Name { get; }

        HashSet<string> Tags { get; }

        object Body { get; }
    }

    public class ReportSectionDto<T> : IReportSectionDto where T : CustomSection
    {
        public ReportSectionDto(T model, Func<T, object> createBody)
        {
            Name = model.Name;
            Tags = model.Tags;
            Body = createBody(model);
        }

        public string Name { get; }

        public HashSet<string> Tags { get; }

        public object Body { get; }

        public static ReportSectionDto<T> Create(T model, Func<T, object> createBody) => new ReportSectionDto<T>(model, createBody);
    }

    public static class ReportSectionDto
    {
        public static IReportSectionDto Create<T>(T model, Func<T, object> createBody) where T : CustomSection
        {
            return new ReportSectionDto<T>(model, createBody);
        }
    }
}