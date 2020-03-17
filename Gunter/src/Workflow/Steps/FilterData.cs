using System.Threading.Tasks;
using Gunter.Workflow.Data;
using Reusable.Extensions;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Annotations;
using Reusable.Flowingo.Data;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Nodes;

namespace Gunter.Workflow.Steps
{
    internal class FilterData : Step<TestContext>
    {
        protected override Task<Flow> ExecuteBody(TestContext context)
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

            return Flow.Continue.ToTask();
        }

    }
}