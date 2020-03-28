using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Gunter.Data.Abstractions;
using Gunter.Data.Configuration;
using Gunter.Data.Configuration.Abstractions;
using Gunter.Data.Configuration.Tasks;
using Gunter.Data.Properties;
using Gunter.Helpers;
using Gunter.Services.Abstractions;
using JetBrains.Annotations;
using Reusable.Extensions;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Nodes;
using Reusable.Translucent;

namespace Gunter.Services.Tasks
{
    [PublicAPI]
    public class ExecuteSendEmail : IExecuteTask<SendEmail>
    {
        public ExecuteSendEmail
        (
            ILogger<ExecuteSendEmail> logger,
            IResource resource,
            ILifetimeScope lifetimeScope,
            IMergeScalar mergeScalar,
            IMergeCollection mergeCollection,
            ITryGetFormatValue tryGetFormatValue,
            Theory theory
        )
        {
            Logger = logger;
            Resource = resource;
            LifetimeScope = lifetimeScope;
            MergeScalar = mergeScalar;
            MergeCollection = mergeCollection;
            TryGetFormatValue = tryGetFormatValue;
            Theory = theory;
        }

        private ILogger<ExecuteSendEmail> Logger { get; }

        private IResource Resource { get; }

        private ILifetimeScope LifetimeScope { get; }

        private IMergeScalar MergeScalar { get; }

        private IMergeCollection MergeCollection { get; }

        private ITryGetFormatValue TryGetFormatValue { get; }

        private Theory Theory { get; }

        public async Task InvokeAsync(SendEmail sendEmail)
        {
            var report = Theory.Reports.Single(r => r.Name.Equals(sendEmail.ReportName));

            using var emailScope = Logger.BeginScope<ExecuteSendEmail>(new { reportName = report.Name });
            using var lifetimeScope = LifetimeScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(report).As<IReport>();
                builder.Register(c => c.Resolve<InstanceProperty<IReport>.Factory>()(x => x.Title)).As<IProperty>();
            });

            try
            {
                var sections = RenderSections(report).ToList();
                await lifetimeScope.Resolve<SendEmailWithMailr>().ExecuteAsync(sendEmail, sections);
            }
            catch (Exception ex)
            {
                Logger.Scope().Exceptions.Push(ex);
            }
        }

        private IEnumerable<IReportSectionDto> RenderSections(IReport report)
        {
            var sections = report.Resolve(x => x.Modules, MergeScalar, modules => modules.Any());
            foreach (var section in sections)
            {
                yield return LifetimeScope.Execute<IReportSectionDto>(typeof(IRenderReportSection<>), section);
            }
        }
    }
}