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

namespace Gunter.Messaging
{
    public interface IAlert
    {
        [JsonRequired]
        int Id { get; set; }

        [JsonRequired]
        List<int> Reports { get; set; }

        void Publish(TestContext context);
    }

    public abstract class Alert : IAlert
    {
        protected Alert([NotNull] ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected ILogger Logger { get; }

        public int Id { get; set; }

        public List<int> Reports { get; set; }

        public void Publish(TestContext context)
        {
            var alert = context.Alerts.Single(x => x.Id == Id);

            LogEntry.New().Debug().Message($"Alert: {alert.Id}").Log(Logger);

            var reports =
                from id in alert.Reports
                join report in context.Reports on id equals report.Id
                select report;

            foreach (var report in reports)
            {
                LogEntry.New().Debug().Message($"Section count: {report.Sections.Count}").Log(Logger);

                PublishCore(context, report);
            }
        }

        protected abstract void PublishCore(TestContext context, IReport report);
    }
}
