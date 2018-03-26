using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gunter.Data;
using Gunter.Reporting;
using JetBrains.Annotations;
using MailrNET;
using MailrNET.API;
using MailrNET.Models;
using Newtonsoft.Json;
using Reusable;
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
        private readonly IMailrClient _mailrClient;

        public HtmlEmail(
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IMailrClient mailrClient,
            IEnumerable<IModuleFactory> moduleFactories)
            : base(loggerFactory)
        {
            _configuration = configuration;
            _mailrClient = mailrClient;
            _moduleFactories = moduleFactories;
        }

        [JsonRequired]
        public string To { get; set; }

        [DefaultValue("Default.css")]
        public string Theme { get; set; }

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

            foreach (var module in report.Modules)
            {
                var moduleFactory = _moduleFactories.Single(r => r.CanCreate(module));
                foreach (var moduleObj in moduleFactory.Create(module, context))
                {
                    body.Add(moduleObj);
                }
            }

            //Logger.Log(Abstraction.Layer.Business().Data().Property(new { To }));

            var to = format(To);
            var subject = format(report.Title);
            var body = new
            {
                Theme,
                Message = new
                {
                    Level = new
                    {

                    }
                }
            };


            await _mailrClient.Emails("Gunter").SendAsync("RunTest", "Result", Email.Create(to, subject, body), CancellationToken.None);

        }
    }
}
