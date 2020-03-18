using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Gunter.Data.Configuration;
using Gunter.Data.Configuration.Abstractions;
using Gunter.Services;
using Gunter.Services.Abstractions;
using Gunter.Workflow.Data;
using Reusable.Extensions;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Data;

namespace Gunter.Workflow.Steps.TestCaseSteps
{
    internal class ProcessMessages : Step<TestContext>
    {
        public ProcessMessages(IComponentContext componentContext)
        {
            ComponentContext = componentContext;
        }

        private IComponentContext ComponentContext { get; }

        public List<IServiceMapping> ServiceMappings { get; set; } = new List<IServiceMapping>();

        protected override async Task<Flow> ExecuteBody(TestContext context)
        {
            if (context.TestCase.Messages.TryGetValue(context.Result, out var messages))
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

    internal interface IServiceMapping
    {
        Type HandleeType { get; }

        Type HandlerType { get; }
    }

    internal class Handle<THandlee> : IServiceMapping
    {
        public static IServiceMapping With<THandler>() => new Handle<THandlee> { HandlerType = typeof(THandler) };

        public Type HandleeType => typeof(THandlee);
        public Type HandlerType { get; private set; }
    }
}