using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Data;
using Gunter.Reporting;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable.OmniLog;

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
            //Logger.Log(e => e.Debug().Message($"Publishing alert {Id}."));

            var reports =
                from id in ReportIds
                join report in context.TestFile.Reports on id equals report.Id
                select report;

            foreach (var report in reports)
            {
                //var logger = Logger.BeginLog(e => e.Stopwatch(sw => sw.Start()));

                try
                {
                    await PublishReport(report, context);
                    //logger.LogEntry.Info().Message($"Published report {report.Id}.");
                }
                catch (Exception ex)
                {
                    //logger.LogEntry.Error().Exception(ex).Message($"Could not publish report {report.Id}.");
                }
                finally
                {
                    //logger.EndLog();
                }
            }
        }

        protected abstract Task PublishReport(IReport report, TestContext context);
    }
}
