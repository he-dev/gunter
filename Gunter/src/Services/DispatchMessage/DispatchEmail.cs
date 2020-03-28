using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Autofac;
using Gunter.Annotations;
using Gunter.Data.Configuration;
using Gunter.Data.Configuration.Abstractions;
using Gunter.Data.Configuration.Reports.CustomSections;
using Gunter.Data.Configuration.Tasks;
using Gunter.Helpers;
using Gunter.Services.Abstractions;
using Gunter.Services.Reporting;
using Gunter.Services.Reporting.Tables;
using JetBrains.Annotations;
using Reusable.Extensions;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Extensions;
using Reusable.OmniLog.Nodes;
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
            IMergeScalar mergeScalar,
            IMergeCollection mergeCollection,
            ITryGetFormatValue tryGetFormatValue,
            Theory theory
        )
        {
            Logger = logger;
            Resource = resource;
            ComponentContext = componentContext;
            MergeScalar = mergeScalar;
            MergeCollection = mergeCollection;
            TryGetFormatValue = tryGetFormatValue;
            Theory = theory;
        }

        private ILogger<DispatchEmail> Logger { get; }

        private IResource Resource { get; }

        private IComponentContext ComponentContext { get; }

        private IMergeScalar MergeScalar { get; }

        private IMergeCollection MergeCollection { get; }

        private ITryGetFormatValue TryGetFormatValue { get; }

        private Theory Theory { get; }

        public GetHandlers GetHandlers { get; set; } = new GetHandlers
        {
            Handle<Heading>.With<RenderHeading>(),
            Handle<Paragraph>.With<RenderParagraph>(),
            Handle<QuerySummary>.With<RenderQuerySummary>(),
            Handle<DataSummary>.With<RenderDataSummary>(),
            Handle<TestSummary>.With<RenderTestSummary>(),
        };

        public async Task InvokeAsync(ITask task) => await InvokeAsync(task as Email);

        public async Task InvokeAsync(Email email)
        {
            var report = Theory.Reports.Single(r => r.Name.Equals(email.ReportName));

            using var emailScope = Logger.BeginScope(nameof(DispatchEmail), new { reportName = report.Name });

            try
            {
                var modules =
                    from module in report.Resolve(x => x.Modules, MergeScalar, modules => modules.Any())
                    let handlerType = GetHandlers.For(module).Single()
                    let render = (IRenderReportModule)ComponentContext.Resolve(handlerType)
                    select render.Execute(module);

                modules = modules.ToList();

                await SendAsync(email, report.Title, modules);
            }
            catch (Exception ex)
            {
                Logger.Scope().Exceptions.Push(ex);
            }
        }

        private async Task SendAsync(Email email, string title, IEnumerable<IReportModuleDto> modules)
        {
            var to = email.To.Select(x => x.Format(TryGetFormatValue));
            var subject = title.Format(TryGetFormatValue);

            var htmlEmail = new Reusable.Utilities.Mailr.Models.Email.Html(to, subject)
            {
                //Theme = Theme,
                //CC = CC,
                Body = new
                {
                    Modules = modules
                },
            };

            Logger.Log(Telemetry.Collect.Application().Metadata("Email", new { htmlEmail.To, htmlEmail.CC, htmlEmail.Subject }));

            var testResultPath = await Resource.ReadSettingAsync(MailrConfig.TestResultPath);
            await Resource.SendEmailAsync(testResultPath, htmlEmail, http =>
            {
                http.UserAgent = new ProductInfoHeaderValue(ProgramInfo.Name, ProgramInfo.Version);
                http.ControllerName = "Mailr";
            });
        }
    }
}