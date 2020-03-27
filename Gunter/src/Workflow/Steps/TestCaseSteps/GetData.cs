using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Gunter.Data.Abstractions;
using Gunter.Data.Configuration.Sections;
using Gunter.Services.Queries;
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

        public ServiceMappingCollection QueryMappings { get; set; } = new ServiceMappingCollection
        {
            Handle<TableOrView>.With<GetDataFromTableOrView>()
        };

        protected override async Task<Flow> ExecuteBody(TestContext context)
        {
            Logger.Log(Telemetry.Collect.Application().WorkItem("Query", new { context.Query.Name }));
            try
            {
                (context.QueryCommand, context.Data) = await Cache.GetOrCreateAsync($"{context.Theory.Name}.{context.Query.Name}", async entry =>
                {
                    var getData = (IGetData)ComponentContext.Resolve(QueryMappings.Map(context.Query).Single());
                    return await getData.ExecuteAsync(context.Query);
                });
                context.GetDataElapsed = Logger.Scope().Stopwatch().Elapsed;
                Logger.Log(Telemetry.Collect.Dependency().Database().Metric("RowCount", context.Data.Rows.Count));
                return Flow.Continue;
            }
            catch (Exception inner)
            {
                throw DynamicException.Create(GetType().ToPrettyString(), $"Error executing query '{context.Query.Name}'.", inner);
            }
        }
    }
}