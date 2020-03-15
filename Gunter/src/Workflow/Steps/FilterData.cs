using System.Threading.Tasks;
using Gunter.Workflows;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Annotations;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Nodes;

namespace Gunter.Workflow.Steps
{
    internal class FilterData : Step<TestContext>
    {
        [Service]
        public ILogger<FilterData> Logger { get; set; }

        public override async Task ExecuteAsync(TestContext context)
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

            await ExecuteNextAsync(context);
        }
    }
}