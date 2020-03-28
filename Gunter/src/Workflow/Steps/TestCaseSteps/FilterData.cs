using System;
using System.Data;
using System.Threading.Tasks;
using Gunter.Workflow.Data;
using Reusable.Extensions;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Data;
using Reusable.OmniLog.Extensions;
using Reusable.OmniLog.Nodes;

namespace Gunter.Workflow.Steps.TestCaseSteps
{
    internal class FilterData : Step<TestContext>
    {
        protected override Task<Flow> ExecuteBody(TestContext context)
        {
            if (context.Query.Filters is {} filters)
            {
                foreach (var dataRow in context.Data.AsEnumerable())
                {
                    foreach (var dataFilter in filters)
                    {
                        dataFilter.Execute(context.Data, dataRow);
                    }
                }

                context.FilterDataElapsed = Logger?.Scope().Stopwatch().Elapsed ?? TimeSpan.Zero;
            }

            return Flow.Continue.ToTask();
        }
    }
}