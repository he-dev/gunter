using System;
using System.Threading.Tasks;
using Gunter.Data;
using Gunter.Workflow.Data;
using Reusable.Exceptionize;
using Reusable.Extensions;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Data;
using Reusable.OmniLog.Nodes;
using Reusable.OmniLog.Extensions;

namespace Gunter.Workflow.Steps.TestCaseSteps
{
    internal class EvaluateData : Step<TestContext>
    {
        protected override Task<Flow> ExecuteBody(TestContext context)
        {
            if (context.Data.Compute(context.TestCase.Assert, context.TestCase.Filter) is bool success)
            {
                context.EvaluateDataElapsed = Logger?.Scope().Stopwatch().Elapsed ?? TimeSpan.Zero;
                context.Result = success switch
                {
                    true => TestResult.Passed,
                    false => TestResult.Failed
                };

                Logger?.Log(Telemetry.Collect.Business().Metadata("TestResult", context.Result));
            }
            else
            {
                throw DynamicException.Create("Assert", $"Could not evaluate test '{context.TestCase.Name}' because it's condition does not yield a '{nameof(Boolean)}'.");
            }

            return Flow.Continue.ToTask();
        }
    }
}