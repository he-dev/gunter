using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Services.Abstractions;
using Gunter.Workflow.Data;
using Gunter.Helpers;
using Reusable.Extensions;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Data;
using Reusable.OmniLog.Extensions;
using Reusable.OmniLog.Nodes;

namespace Gunter.Workflow.Steps.TestCaseSteps
{
    internal class FilterData : Step<TestContext>
    {
        public FilterData(IMergeProvider mergeProvider)
        {
            MergeProvider = mergeProvider;
        }

        private IMergeProvider MergeProvider { get; }

        protected override Task<Flow> ExecuteBody(TestContext context)
        {
            if (context.Query.Resolve(x => x.Filters, MergeProvider.Scalar, x => x.Any()) is {} filters)
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