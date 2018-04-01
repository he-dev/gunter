using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gunter.Data;
using Gunter.Messaging.Emails.Internal;
using Gunter.Reporting;
using JetBrains.Annotations;
using MailrNET;
using MailrNET.API;
using MailrNET.Models;
using Newtonsoft.Json;
using Reusable.Extensions;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.SmartConfig;
using Reusable.SmartConfig.Utilities;

namespace Gunter.Messaging.Emails
{
    [PublicAPI]
    public class HtmlEmail : Message
    {
        private readonly IConfiguration _configuration;
        private readonly IEnumerable<IModuleFactory> _moduleFactories;
        private readonly Factory _factory;
        private readonly IMailrClient _mailrClient;

        public delegate HtmlEmail Factory();

        internal HtmlEmail(
            ILogger<HtmlEmail> logger,
            IConfiguration configuration,
            IMailrClient mailrClient,
            IEnumerable<IModuleFactory> moduleFactories,
            Factory factory
        ) : base(logger)
        {
            _configuration = configuration;
            _mailrClient = mailrClient;
            _moduleFactories = moduleFactories;
            _factory = factory;
        }

        public string To { get; set; }

        [DefaultValue("Default.css")]
        public string Theme { get; set; }

        public override IMergable New()
        {
            var mergable = _factory();
            mergable.Id = Id;
            mergable.Merge = Merge;
            return mergable;
        }

        protected override async Task PublishReportAsync(IReport report, TestContext context)
        {
            var format = (FormatFunc)context.Formatter.Format;

            Logger.Log(Abstraction.Layer.Business().Data().Argument(new
            {
                report = new
                {
                    Type = report.GetType().ToPrettyString(),
                    ModuleCount = report.Modules.Count,
                    ModuleTypes = report.Modules.Select(m => m.GetType().Name)
                }
            }));

            var modules =
                from module in report.Modules
                let moduleObject = _moduleFactories.Single(r => r.CanCreate(module)).Create(module, context)
                select (module.GetType().Name, moduleObject);

            //Logger.Log(Abstraction.Layer.Business().Data().Property(new { To }));

            var to = format(To);
            var subject = format(report.Title);
            var body = new
            {
                Theme,
                Modules = modules.ToDictionary(t => t.Name, t => t.moduleObject)
            };

            await _mailrClient.Emails("Gunter").SendAsync("RunTest", "Result", Email.Create(to, subject, body), CancellationToken.None);
        }
    }
}
