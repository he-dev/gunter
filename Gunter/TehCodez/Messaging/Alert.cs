using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gunter.Data;
using Gunter.Messaging.Email;
using Gunter.Reporting;
using Gunter.Services;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;
using Reusable.Logging;
using Reusable.Logging.Loggex;

namespace Gunter.Messaging
{
    public interface IAlert : IResolvable
    {
        [JsonRequired]
        int Id { get; set; }

        [JsonRequired]
        List<int> Reports { get; set; }

        void Publish(TestUnit context);
    }

    public abstract class Alert : IAlert
    {
        protected Alert([NotNull] ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [JsonIgnore]
        public IVariableResolver Variables { get; set; } = VariableResolver.Empty;

        protected ILogger Logger { get; }

        public int Id { get; set; }

        public List<int> Reports { get; set; } = new List<int>();

        public void Publish(TestUnit context)
        {
            Logger.Log(e => e.Debug().Message($"Publishing alert {Id}."));

            var reports =
                from id in Reports
                join report in context.Reports on id equals report.Id
                select report.UpdateVariables(Variables);

            foreach (var report in reports)
            {
                var logger = Logger.BeginLog(e => e.Stopwatch(sw => sw.Start()));

                try
                {
                    PublishCore(context, report);
                    logger.LogEntry.Info().Message($"Published report {report.Id}.");
                }
                catch (Exception ex)
                {
                    logger.LogEntry.Error().Exception(ex).Message($"Could not publish report {report.Id}.");
                }
                finally
                {
                    logger.EndLog();
                }
            }
        }

        protected abstract void PublishCore(TestUnit testUnit, IReport report);
    }
}
