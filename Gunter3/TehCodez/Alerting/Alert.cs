using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Gunter.Data;
using Gunter.Reporting;
using Gunter.Services;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable.OmniLog;

namespace Gunter.Alerting
{
    public interface IAlert
    {
        [JsonRequired]
        int Id { get; set; }

        [JsonRequired]
        [JsonProperty("Reports")]
        List<int> ReportIds { get; set; }

        void Publish(TestContext context);
    }

    public abstract class Alert : IAlert
    {
        protected Alert([NotNull] ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger(GetType().Name);
        }

        protected ILogger Logger { get; }

        public int Id { get; set; }

        public List<int> ReportIds { get; set; } = new List<int>();

        public void Publish(TestContext context)
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
                    PublishCore(report, context);
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

        protected abstract void PublishCore(IReport report, TestContext context);
    }
}
