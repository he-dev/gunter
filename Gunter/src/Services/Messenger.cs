using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Data;
using Gunter.Reporting;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Nodes;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter.Services
{
    public interface IChannel : IMergeable
    {
        Task SendAsync(TestContext context, IEnumerable<SoftString> reportIds);
    }

    public abstract class Channel : IChannel
    {
        protected Channel([NotNull] ILogger logger)
        {
            Logger = logger;
        }

        protected ILogger Logger { get; }

        [JsonRequired]
        public SoftString Id { get; set; }

        public Merge? Merge { get; set; }

        public async Task SendAsync(TestContext context, IEnumerable<SoftString> reportIds)
        {
            var reports =
                from id in reportIds
                join report in context.TestBundle.Reports on id equals report.Id
                select report;

            foreach (var report in reports)
            {
                using (Logger.BeginScope().WithCorrelationHandle("PublishReport").UseStopwatch())
                {
                    Logger.Log(Abstraction.Layer.Service().Meta(new { ReportId = report.Id }));
                    try
                    {
                        var modules =
                            from module in report.Modules
                            select module.CreateDto(context);

                        await PublishReportAsync(context, report, modules);
                        Logger.Log(Abstraction.Layer.Network().Routine(Logger.Scope().CorrelationHandle.ToString()).Completed());
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(Abstraction.Layer.Network().Routine(Logger.Scope().CorrelationHandle.ToString()).Faulted(ex));
                    }
                }
            }
        }

        protected abstract Task PublishReportAsync(TestContext context, IReport report, IEnumerable<IModuleDto> modules);
    }
}