using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Annotations;
using Gunter.Data;
using Gunter.Data.Dtos;
using Gunter.Reporting;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter.Services
{
    public interface IMessenger : IMergeable
    {
        //[JsonProperty("Reports")]
        //IList<SoftString> ReportIds { get; set; }

        //Task SendAsync(TestContext context);

        Task SendAsync(TestContext context, IEnumerable<SoftString> reportIds);
    }

    public abstract class Messenger : IMessenger
    {
        protected Messenger([NotNull] ILogger logger)
        {
            Logger = logger;
        }

        protected ILogger Logger { get; }

        [JsonRequired]
        public SoftString Id { get; set; }

        public Merge Merge { get; set; }

        //[Mergeable]
        //public IList<SoftString> ReportIds { get; set; } = new List<SoftString>();

//        public async Task SendAsync(TestContext context)
//        {
//            var reports =
//                from id in ReportIds
//                join report in context.TestBundle.Reports on id equals report.Id
//                select report;
//
//            foreach (var report in reports)
//            {
//                using (Logger.BeginScope().WithCorrelationHandle("Report").AttachElapsed())
//                {
//                    Logger.Log(Abstraction.Layer.Infrastructure().Meta(new { ReportId = report.Id }));
//                    try
//                    {
//                        var sections =
//                            from module in report.Modules
//                            let dto = module.CreateDto(context)
//                            select (module.GetType().Name, dto);
//
//                        await PublishReportAsync(context, report, sections);
//                        Logger.Log(Abstraction.Layer.Network().Routine(nameof(SendAsync)).Completed());
//                    }
//                    catch (Exception ex)
//                    {
//                        Logger.Log(Abstraction.Layer.Network().Routine(nameof(SendAsync)).Faulted(), ex);
//                    }
//                }
//            }
//        }
        
        public async Task SendAsync(TestContext context, IEnumerable<SoftString> reportIds)
        {
            var reports =
                from id in reportIds
                join report in context.TestBundle.Reports on id equals report.Id
                select report;

            foreach (var report in reports)
            {
                using (Logger.BeginScope().WithCorrelationHandle("Report").AttachElapsed())
                {
                    Logger.Log(Abstraction.Layer.Infrastructure().Meta(new { ReportId = report.Id }));
                    try
                    {
                        var sections =
                            from module in report.Modules
                            let dto = module.CreateDto(context)
                            select (module.GetType().Name, dto);

                        await PublishReportAsync(context, report, sections);
                        Logger.Log(Abstraction.Layer.Network().Routine(nameof(SendAsync)).Completed());
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(Abstraction.Layer.Network().Routine(nameof(SendAsync)).Faulted(), ex);
                    }
                }
            }
        }

        protected abstract Task PublishReportAsync(TestContext context, IReport report, IEnumerable<(string Name, ModuleDto Section)> sections);
    }
}