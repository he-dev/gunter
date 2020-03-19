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
using Reusable.OmniLog;
using Reusable.OmniLog.Nodes;
using Reusable.OmniLog.SemanticExtensions;

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
            using var scope = Logger.BeginScope().WithCorrelationHandle(nameof(GetData)).UseStopwatch();
            Logger.Log(Abstraction.Layer.Service().Subject(new { QueryId = context.Query.Name }));
            try
            {
                (context.QueryCommand, context.Data) = await Cache.GetOrCreateAsync($"{context.Theory.Name}.{context.Query.Name}", async entry =>
                {
                    var getData = (IGetData)ComponentContext.Resolve(QueryMappings.Map(context.Query).Single());
                    return await getData.ExecuteAsync(context.Query);
                });
                context.GetDataElapsed = Logger.Scope().Stopwatch().Elapsed;
                Logger.Log(Abstraction.Layer.Database().Counter(new { RowCount = context.Data.Rows.Count, ColumnCount = context.Data.Columns.Count }));
                return Flow.Continue;
            }
            catch (Exception inner)
            {
                throw DynamicException.Create(GetType().ToPrettyString(), $"Error getting or processing data for query '{context.Query.Name}'.", inner);
            }
        }
    }
}