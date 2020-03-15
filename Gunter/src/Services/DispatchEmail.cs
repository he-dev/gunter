using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Gunter.Annotations;
using Gunter.Data.Configuration;
using Gunter.Data.Configuration.Abstractions;
using Gunter.Data.Configuration.Reporting.Abstractions;
using Gunter.Services.Abstractions;
using JetBrains.Annotations;
using Reusable.Extensions;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.Translucent;
using Reusable.Utilities.Mailr;
using Email = Reusable.Utilities.Mailr.Models.Email;

namespace Gunter.Services
{
    [Gunter]
    [PublicAPI]
    public class DispatchEmail : DispatchMessage
    {
        public DispatchEmail
        (
            ILogger<DispatchEmail> logger,
            IResource resource,
            IComponentContext componentContext,
            Format format,
            Theory theory
        ) : base(logger)
        {
            Logger = logger;
            Resource = resource;
            ComponentContext = componentContext;
            Format = format;
            Theory = theory;
        }

        private ILogger<DispatchEmail> Logger { get; }

        private IResource Resource { get; }

        private IComponentContext ComponentContext { get; }

        private Format Format { get; }

        private Theory Theory { get; }

        public override async Task InvokeAsync(IMessage message)
        {
            var report = Theory.Reports.Single(r => r.Name.Equals(message.ReportName));

            //using (Logger.BeginScope().WithCorrelationHandle("PublishReport").UseStopwatch())
            {
                //Logger.Log(Abstraction.Layer.Service().Meta(new { ReportId = report.Id }));
                try
                {
                    var modules =
                        from module in report.Modules
                        let render = (IRenderReportModule)default(IComponentContext).Resolve(module.GetType().GetCustomAttribute<ServiceAttribute>())
                        select render.Execute(module);


                    await SendAsync((Data.Configuration.Email)message, report.Title, modules);
                    //Logger.Log(Abstraction.Layer.Network().Routine(Logger.Scope().CorrelationHandle.ToString()).Completed());
                }
                catch (Exception ex)
                {
                    //Logger.Log(Abstraction.Layer.Network().Routine(Logger.Scope().CorrelationHandle.ToString()).Faulted(ex));
                }
            }
        }

        private async Task SendAsync(Data.Configuration.Email email, string title, IEnumerable<IReportModuleDto> modules)
        {
            var to = email.To.Select(x => x.Map(Format));
            var subject = title.Map(Format);

            var htmlEmail = new Email.Html(to, subject)
            {
                //Theme = Theme,
                //CC = CC,
                Body = new
                {
                    Modules = modules
                },
            };

            Logger.Log(Abstraction.Layer.Service().Meta(new { email = new { htmlEmail.To, htmlEmail.CC, htmlEmail.Subject } }));

            var testResultPath = await Resource.ReadSettingAsync(MailrConfig.TestResultPath);
            await Resource.SendEmailAsync(testResultPath, htmlEmail, http =>
            {
                http.UserAgent = new ProductInfoHeaderValue(ProgramInfo.Name, ProgramInfo.Version);
                http.ControllerTags.Add("Mailr");
            });
        }
    }
}