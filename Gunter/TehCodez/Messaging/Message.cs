using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Data;
using Gunter.Reporting;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter.Messaging
{
    public interface IMessage
    {
        [JsonRequired]
        int Id { get; set; }

        [JsonRequired]
        [JsonProperty("Reports")]
        List<int> ReportIds { get; set; }

        Task PublishAsync(TestContext context);
    }

    public abstract class Message : IMessage
    {
        protected Message([NotNull] ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger(GetType().Name);
        }

        protected ILogger Logger { get; }

        public int Id { get; set; }

        public List<int> ReportIds { get; set; } = new List<int>();

        public async Task PublishAsync(TestContext context)
        {
            var reports =
                from id in ReportIds
                join report in context.TestFile.Reports on id equals report.Id
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
