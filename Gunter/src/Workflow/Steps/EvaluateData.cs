using System;
using System.Threading.Tasks;
using Gunter.Data;
using Gunter.Data.Configuration;
using Gunter.Workflows;
using Reusable.Exceptionize;
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
        [Service]
        public ILogger<EvaluateData> Logger { get; set; }

        public override async Task ExecuteAsync(TestContext context)
        {
            using var scope = Logger.BeginScope().WithCorrelationHandle(nameof(EvaluateData)).UseStopwatch();

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

            await ExecuteNextAsync(context);
        }
    }
}