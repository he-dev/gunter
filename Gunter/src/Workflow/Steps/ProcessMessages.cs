using System.Threading.Tasks;
using Autofac;
using Gunter.Data.Configuration;
using Gunter.Services;
using Gunter.Workflow.Data;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Data;
using Reusable.OmniLog.Abstractions;

namespace Gunter.Workflow.Steps
{
    internal class ProcessMessages : Step<TestContext>
    {
        public ProcessMessages(IComponentContext componentContext) 
        {
            ComponentContext = componentContext;
        }

        private IComponentContext ComponentContext { get; }

        protected override async Task<Flow> ExecuteBody(TestContext context)
        {
            if (context.TestCase.Messages.TryGetValue(context.Result, out var messages))
            {
                foreach (var message in messages)
                {
                    switch (message)
                    {
                        case Email email:
                            await ComponentContext.Resolve<DispatchEmail>().InvokeAsync(email);
                            break;
                    }
                }
            }

            return Flow.Continue;
        }
    }
}