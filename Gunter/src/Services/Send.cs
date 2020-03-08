using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Data;
using Gunter.Reporting;
using Gunter.Workflows;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Nodes;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter.Services
{
    public interface ISend
    {
        Task InvokeAsync(TestContext context, IEmail email);
    }

    public abstract class Send : ISend
    {
        protected Send(ILogger logger)
        {
            Logger = logger;
        }

        protected ILogger Logger { get; }

        public async Task InvokeAsync(TestContext context, IEmail email)
        {
            var report = context.Theory.Reports.Single(r => r.Name.Equals(email.ReportName));


            using (Logger.BeginScope().WithCorrelationHandle("PublishReport").UseStopwatch())
            {
                Logger.Log(Abstraction.Layer.Service().Meta(new { ReportId = report.Id }));
                try
                {
                    var modules =
                        from module in report.Modules
                        select module.Create(context);

                    //await PublishReportAsync(message, report, modules);
                    Logger.Log(Abstraction.Layer.Network().Routine(Logger.Scope().CorrelationHandle.ToString()).Completed());
                }
                catch (Exception ex)
                {
                    Logger.Log(Abstraction.Layer.Network().Routine(Logger.Scope().CorrelationHandle.ToString()).Faulted(ex));
                }
            }
        }
    }

    public class CreateReport
    {
        public async Task InvokeAsync(Workflows.TestContext context, string testCaseName, string messageName)
        {
            var reports =
                from id in theoryFile.OfType<ITestCase>()
                join report in message.TheoryFile.Reports on id equals report.Id
                select report;

            foreach (var report in reports)
            {
                using (Logger.BeginScope().WithCorrelationHandle("PublishReport").UseStopwatch())
                {
                    Logger.Log(Abstraction.Layer.Service().Meta(new { ReportId = report.Id }));
                    try
                    {
                        var modules =
                            from module in report.Modules
                            select module.CreateDto(message);

                        await PublishReportAsync(message, report, modules);
                        Logger.Log(Abstraction.Layer.Network().Routine(Logger.Scope().CorrelationHandle.ToString()).Completed());
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(Abstraction.Layer.Network().Routine(Logger.Scope().CorrelationHandle.ToString()).Faulted(ex));
                    }
                }
            }
        }

        protected abstract Task PublishReportAsync(TestContext context, IReport report, IEnumerable<IReportModule> modules);
    }
}