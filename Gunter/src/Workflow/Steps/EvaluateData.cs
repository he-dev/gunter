using System;
using System.Threading.Tasks;
using Gunter.Data;
using Gunter.Data.Configuration;
using Gunter.Workflow.Data;
using Reusable.Exceptionize;
using Reusable.Extensions;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Annotations;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Nodes;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter.Workflow.Steps
{
    internal class EvaluateData : Step<TestContext>
    {
        public EvaluateData(ILogger<EvaluateData> logger) : base(logger) { }

        protected override Task<bool> ExecuteBody(TestContext context)
        {
            if (context.Data.Compute(context.TestCase.Assert, context.TestCase.Filter) is bool success)
            {
                context.EvaluateDataElapsed = Logger.Scope().Stopwatch().Elapsed;
                context.Result = success switch
                {
                    true => TestResult.Passed,
                    false => TestResult.Failed
                };

                Logger.Log(Abstraction.Layer.Service().Meta(new { TestResult = context.Result }));
            }
            else
            {
                throw DynamicException.Create("Assert", $"'{nameof(TestCase.Assert)}' must evaluate to '{nameof(Boolean)}'.");
            }

            return true.ToTask();
        }
    }
}