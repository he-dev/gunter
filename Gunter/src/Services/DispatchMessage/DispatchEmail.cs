﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Autofac;
using Gunter.Annotations;
using Gunter.Data.Configuration;
using Gunter.Data.Configuration.Abstractions;
using Gunter.Data.Configuration.Reporting;
using Gunter.Services.Abstractions;
using Gunter.Services.Reporting;
using Gunter.Workflow.Steps.TestCaseSteps;
using JetBrains.Annotations;
using Reusable.Extensions;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Nodes;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.Translucent;
using Reusable.Utilities.Mailr;

namespace Gunter.Services.DispatchMessage
{
    [Gunter]
    [PublicAPI]
    public class DispatchEmail : IDispatchMessage
    {
        public DispatchEmail
        (
            ILogger<DispatchEmail> logger,
            IResource resource,
            IComponentContext componentContext,
            Format format,
            Theory theory
        )
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

        public ServiceMappingCollection ServiceMappings { get; set; } = new ServiceMappingCollection
        {
            Handle<Heading>.With<RenderHeading>(),
            Handle<Paragraph>.With<RenderParagraph>(),
            Handle<QuerySummary>.With<RenderQuerySummary>(),
            Handle<DataSummary>.With<RenderDataSummary>(),
            Handle<TestSummary>.With<RenderTestSummary>(),
        };

        public async Task InvokeAsync(IMessage message)
        {
            var report = Theory.Reports.Single(r => r.Name.Equals(message.ReportName));

            using var loggerScope = Logger.BeginScope().WithCorrelationHandle(nameof(DispatchEmail)).UseStopwatch();
            Logger.Log(Abstraction.Layer.Service().Meta(new { reportName = report.Name }));
            
            try
            {
                var modules =
                    from module in report.Modules
                    let handlerType = ServiceMappings.Map(module).Single()
                    let render = (IRenderReportModule)ComponentContext.Resolve(handlerType)
                    select render.Execute(module);


                await SendAsync((Email)message, report.Title, modules);
                Logger.Log(Abstraction.Layer.Network().Routine(Logger.Scope().CorrelationHandle.ToString()).Completed());
            }
            catch (Exception ex)
            {
                Logger.Log(Abstraction.Layer.Network().Routine(Logger.Scope().CorrelationHandle.ToString()).Faulted(ex));
            }
        }

        private async Task SendAsync(Email email, string title, IEnumerable<IReportModuleDto> modules)
        {
            var to = email.To.Select(x => x.Map(Format));
            var subject = title.Map(Format);

            var htmlEmail = new Reusable.Utilities.Mailr.Models.Email.Html(to, subject)
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