﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Gunter.Annotations;
using Gunter.Data;
using Gunter.Reporting;
using Gunter.Workflows;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable.Extensions;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.Translucent;
using Reusable.Utilities.Mailr;
using Reusable.Utilities.Mailr.Models;
using Email = Reusable.Utilities.Mailr.Models.Email;

namespace Gunter.Services.Channels
{
    [Gunter]
    [PublicAPI]
    public class SendEmail : Send
    {
        private readonly IResource _resource;

        public SendEmail(ILogger<SendEmail> logger, IResource resource) : base(logger)
        {
            _resource = resource;
        }

        public List<string> To { get; set; } = new List<string>();

        [JsonProperty("Report")]
        public string ReportName { get; set; }

        public override async Task InvokeAsync(TestContext context)
        {
            var report = context.Theory.Reports.Single(r => r.Name.Equals(ReportName));


            //using (Logger.BeginScope().WithCorrelationHandle("PublishReport").UseStopwatch())
            {
                //Logger.Log(Abstraction.Layer.Service().Meta(new { ReportId = report.Id }));
                try
                {
                    var modules =
                        from module in report.Modules
                        select module.Create(context);
                    
                    

                    //await PublishReportAsync(message, report, modules);
                    //Logger.Log(Abstraction.Layer.Network().Routine(Logger.Scope().CorrelationHandle.ToString()).Completed());
                }
                catch (Exception ex)
                {
                    //Logger.Log(Abstraction.Layer.Network().Routine(Logger.Scope().CorrelationHandle.ToString()).Faulted(ex));
                }
            }
        }
        
        private async Task PublishAsync(TestContext context, IReport report, IEnumerable<IReportModule> modules)
        {
            var to = To.Select(x => x.Format(context.Container));
            var subject = report.Title.Format(context.Container);

            modules = modules.ToList();

            var email = new Email.Html(to, subject)
            {
                //Theme = Theme,
                //CC = CC,
                Body = new
                {
                    Modules = modules
                },
            };

            Logger.Log(Abstraction.Layer.Service().Meta(new
            {
                email.To,
                email.CC,
                email.Subject,
                email.Theme,
                Modules = modules.Select(m => m.Name)
            }, "EmailInfo"));

            var testResultPath = await _resource.ReadSettingAsync(MailrConfig.TestResultPath);
            await _resource.SendEmailAsync(testResultPath, email, http =>
            {
                http.UserAgent = new ProductInfoHeaderValue(ProgramInfo.Name, ProgramInfo.Version);
                http.ControllerTags.Add("Mailr");
            });
        }
    }
}