using System.Threading.Tasks;
using Autofac;
using Gunter.Helpers;
using Gunter.Services.Abstractions;
using Gunter.Workflow.Data;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Data;

namespace Gunter.Workflow.Steps.TestCaseSteps
{
    internal class ProcessTasks : Step<TestContext>
    {
        public ProcessTasks(IComponentContext componentContext)
        {
            ComponentContext = componentContext;
        }

        private IComponentContext ComponentContext { get; }

        protected override async Task<Flow> ExecuteBody(TestContext context)
        {
            if (context.TestCase.When.TryGetValue(context.Result, out var tasks))
            {
                foreach (var task in tasks)
                {
                    await ComponentContext.ExecuteAsync(typeof(IExecuteTask<>), task);
                }
            }

            return Flow.Continue;
        }
    }
}