using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Annotations;
using Gunter.Data;
using Gunter.Reporting;
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

        public string Merge { get; set; }

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
                var scope = Logger.BeginScope(nameof(PublishAsync), new { reportId = report.Id }).AttachElapsed();
                try
                {
                    await PublishReportAsync(report, context);
                    Logger.Log(Abstraction.Layer.Network().Action().Finished(nameof(PublishAsync)));
                }
                catch (Exception ex)
                {
                    Logger.Log(Abstraction.Layer.Network().Action().Failed(nameof(PublishAsync)), log => log.Exception(ex));
                }
                finally
                {
                    scope.Dispose();
                }
            }
        }

        protected abstract Task PublishReportAsync(IReport report, TestContext context);
    }
}
