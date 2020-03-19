using System;
using System.Threading.Tasks;
using Gunter.Data;
using Gunter.Data.Configuration.Sections;
using Gunter.Workflow.Data;
using Reusable.Exceptionize;
using Reusable.Extensions;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Data;
using Reusable.OmniLog;
using Reusable.OmniLog.Nodes;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter.Workflow.Steps.TestCaseSteps
{
    internal class EvaluateData : Step<TestContext>
    {
        protected override Task<Flow> ExecuteBody(TestContext context)
        {
            if (context.Data.Compute(context.TestCase.Assert, context.TestCase.Filter) is bool success)
            {
                context.EvaluateDataElapsed = Logger.Scope().Stopwatch().Elapsed;
                context.Result = success switch
                {
                    true => TestResult.Passed,
                    false => TestResult.Failed
                };

                Logger?.Log(Abstraction.Layer.Service().Meta(new { TestResult = context.Result }));
            }
            else
            {
                throw DynamicException.Create("Assert", $"'{nameof(TestCase.Assert)}' must evaluate to '{nameof(Boolean)}'.");
            }

            return Flow.Continue.ToTask();
        }
    }
}