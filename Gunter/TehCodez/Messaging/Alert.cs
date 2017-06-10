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
    public interface IAlert : IResolvable
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

        [JsonIgnore]
        public IConstantResolver Constants { get; set; } = ConstantResolver.Empty;

        protected ILogger Logger { get; }

        public int Id { get; set; }

        public List<int> Reports { get; set; } = new List<int>();

        public void Publish(TestContext context)
        {
            LogEntry.New().Debug().Message($"Publishing alert {Id}").Log(Logger);

            var reports =
                from id in Reports
                join report in context.Reports on id equals report.Id
                select report;

            foreach (var report in reports)
            {
                LogEntry.New().Debug().Message($"Publishing report: {report.Id} with {report.Sections.Count} section(s).").Log(Logger);

                PublishCore(context, report);
            }
        }

        protected abstract void PublishCore(TestContext context, IReport report);
    }
}
