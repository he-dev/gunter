using System.Threading.Tasks;
using Gunter.Workflow.Data;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Annotations;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Nodes;

namespace Gunter.Workflow.Steps
{
    internal class FilterData : Step<TestContext>
    {
        public FilterData(ILogger<FilterData> logger) : base(logger) { }
        
        protected override async Task<bool> ExecuteBody(TestContext context)
        {
            if (context.Query.Filters is {} filters)
            {
                using var scope = Logger.BeginScope().WithCorrelationHandle(nameof(FilterData)).UseStopwatch();
                foreach (var dataFilter in filters)
                {
                    dataFilter.Execute(context.Data);
                }

                context.FilterDataElapsed = Logger.Scope().Stopwatch().Elapsed;
            }

            return true;
        }

    }
}