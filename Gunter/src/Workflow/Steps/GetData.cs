using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Data;
using Gunter.Workflow.Data;
using Microsoft.Extensions.Caching.Memory;
using Reusable.Exceptionize;
using Reusable.Extensions;
using Reusable.Flowingo.Abstractions;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Nodes;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter.Workflow.Steps
{
    internal class GetData : Step<TestContext>
    {
        public GetData(ILogger<GetData> logger, IMemoryCache cache, IEnumerable<IGetData> getDataCommands) : base(logger)
        {
            Cache = cache;
            GetDataCommands = getDataCommands;
        }
        
        private IMemoryCache Cache { get; }

        private IEnumerable<IGetData> GetDataCommands { get; }

        protected override async Task<bool> ExecuteBody(TestContext context)
        {
            using var scope = Logger.BeginScope().WithCorrelationHandle(nameof(GetData)).UseStopwatch();
            Logger.Log(Abstraction.Layer.Service().Subject(new { QueryId = context.Query.Name }));
            try
            {
                (context.QueryCommand, context.Data) = await Cache.GetOrCreateAsync($"{context.Theory.Name}.{context.Query.Name}", async entry =>
                {
                    if (GetDataCommands.Single(o => o.QueryType.IsInstanceOfType(context.Query)) is {} getData)
                    {
                        return await getData.ExecuteAsync(context.Query);
                    }

                    return default;
                });
                context.GetDataElapsed = Logger.Scope().Stopwatch().Elapsed;
                Logger.Log(Abstraction.Layer.Database().Counter(new { RowCount = context.Data.Rows.Count, ColumnCount = context.Data.Columns.Count }));
                return true;
            }
            catch (Exception inner)
            {
                throw DynamicException.Create(GetType().ToPrettyString(), $"Error getting or processing data for query '{context.Query.Name}'.", inner);
            }

            return false;
        }
    }
}