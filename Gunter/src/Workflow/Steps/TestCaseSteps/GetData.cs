using System;
using System.Threading.Tasks;
using Autofac;
using Gunter.Data.Abstractions;
using Gunter.Helpers;
using Gunter.Workflow.Data;
using Microsoft.Extensions.Caching.Memory;
using Reusable.Exceptionize;
using Reusable.Extensions;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Data;
using Reusable.OmniLog.Nodes;
using Reusable.OmniLog.Extensions;

namespace Gunter.Workflow.Steps.TestCaseSteps
{
    internal class GetData : Step<TestContext>
    {
        public GetData(IMemoryCache cache, IComponentContext componentContext)
        {
            Cache = cache;
            ComponentContext = componentContext;
        }

        private IMemoryCache Cache { get; }

        private IComponentContext ComponentContext { get; }

        protected override async Task<Flow> ExecuteBody(TestContext context)
        {
            Logger?.Log(Telemetry.Collect.Application().WorkItem("Query", new { context.Query.Name }));
            try
            {
                var (command, data) = await Cache.GetOrCreateAsync($"{context.Theory.Name}.{context.Query.Name}", GetEntryData(context.Query));
                context.QueryCommand = command;
                context.Data = data;
                context.GetDataElapsed = Logger?.Scope().Stopwatch().Elapsed ?? TimeSpan.Zero;
                Logger?.Log(Telemetry.Collect.Dependency().Database().Metric("RowCount", context.Data.Rows.Count));
            }
            catch (Exception inner)
            {
                throw DynamicException.Create(GetType().ToPrettyString(), $"Error executing query '{context.Query.Name}'.", inner);
            }

            return Flow.Continue;
        }

        private Func<ICacheEntry, Task<GetDataResult>> GetEntryData(IQuery query)
        {
            return async _ => await ComponentContext.ExecuteAsync<GetDataResult>(typeof(IGetData<>), query);
        }
    }
}