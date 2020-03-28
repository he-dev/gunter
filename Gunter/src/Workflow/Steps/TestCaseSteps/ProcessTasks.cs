using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Gunter.Data.Abstractions;
using Gunter.Data.Configuration.Tasks;
using Gunter.Services;
using Gunter.Services.Abstractions;
using Gunter.Services.DispatchMessage;
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

        public List<IServiceMapping> ServiceMappings { get; set; } = new List<IServiceMapping>()
        {
            Handle<Email>.With<DispatchEmail>(),
            Handle<Halt>.With<ThrowOperationCanceledException>()
        };

        protected override async Task<Flow> ExecuteBody(TestContext context)
        {
            if (context.TestCase.When.TryGetValue(context.Result, out var messages))
            {
                foreach (var message in messages)
                {
                    var dispatchType = ServiceMappings.Single(m => m.HandleeType.IsInstanceOfType(message)).HandlerType;
                    var dispatchMessage = (IDispatchMessage)ComponentContext.Resolve(dispatchType);
                    await dispatchMessage.InvokeAsync(message);
                }
            }

            return Flow.Continue;
        }
    }
}