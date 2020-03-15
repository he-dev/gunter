using System.Threading.Tasks;
using Autofac;
using Gunter.Data.Configuration;
using Gunter.Services;
using Gunter.Workflows;
using Reusable.Flowingo.Abstractions;
using Reusable.OmniLog.Abstractions;

namespace Gunter.Workflow.Steps
{
    internal class SendMessages : Step<TestContext>
    {
        public SendMessages(ILogger<SendMessages> logger, IComponentContext componentContext)
        {
            Logger = logger;
            ComponentContext = componentContext;
        }

        private ILogger<SendMessages> Logger { get; set; }

        private IComponentContext ComponentContext { get; }

        public override async Task ExecuteAsync(TestContext context)
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

            await ExecuteNextAsync(context);
        }
    }
}