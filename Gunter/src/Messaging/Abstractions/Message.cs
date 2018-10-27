using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter.Messaging.Abstractions
{
    public interface IMessage : IMergeable
    {
        [JsonProperty("Reports")]
        IList<SoftString> ReportIds { get; set; }

        Task PublishAsync(TestContext context);
    }

    public abstract class Message : IMessage
    {
        protected Message([NotNull] ILogger logger)
        {
            Logger = logger;
        }

        protected ILogger Logger { get; }

        [JsonRequired]
        public SoftString Id { get; set; }

        public Merge Merge { get; set; }

        [Mergable]
        public IList<SoftString> ReportIds { get; set; } = new List<SoftString>();

        public async Task PublishAsync(TestContext context)
        {
            var reports =
                from id in ReportIds
                join report in context.TestBundle.Reports on id equals report.Id
                select report;

            foreach (var report in reports)
            {
                using (Logger.BeginScope().WithCorrelationContext(new { reportId = report.Id }).AttachElapsed())
                {
                    try
                    {
                        var dtos =
                            from module in report.Modules
                            let dto = module.CreateDto(context)
                            select (module.GetType().Name, dto);
                        
                        await PublishReportAsync(context, report, dtos);
                        Logger.Log(Abstraction.Layer.Network().Routine(nameof(PublishAsync)).Completed());
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(Abstraction.Layer.Network().Routine(nameof(PublishAsync)).Faulted(), ex);
                    }
                }
            }
        }

        protected abstract Task PublishReportAsync(TestContext context, IReport report, IEnumerable<(string Name, SectionDto Section)> sections);
    }
}
