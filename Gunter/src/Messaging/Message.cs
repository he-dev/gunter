using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Annotations;
using Gunter.Data;
using Gunter.Reporting;
using Gunter.Services;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter.Messaging
{
    public interface IMessage : IMergable
    {
        [JsonProperty("Reports")]
        List<int> ReportIds { get; set; }

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
        public int Id { get; set; }

        public Merge Merge { get; set; }

        [Mergable]
        public List<int> ReportIds { get; set; } = new List<int>();

        public abstract IMergable New();

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
                        await PublishReportAsync(context, report);
                        Logger.Log(Abstraction.Layer.Network().Routine(nameof(PublishAsync)).Completed());
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(Abstraction.Layer.Network().Routine(nameof(PublishAsync)).Faulted(), ex);
                    }
                }
            }
        }

        protected abstract Task PublishReportAsync(TestContext context, IReport report);
    }
}
