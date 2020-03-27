using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Gunter.Data;
using Gunter.Data.Configuration;
using Gunter.Workflow.Data;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Data;
using Reusable.Flowingo.Steps;
using Reusable.OmniLog;
using Reusable.OmniLog.Nodes;

namespace Gunter.Workflow.Steps.SessionSteps
{
    using static TheoryType;

    internal class ProcessTheories : Step<SessionContext>
    {
        public ProcessTheories(ILifetimeScope lifetimeScope)
        {
            LifetimeScope = lifetimeScope;
        }

        private ILifetimeScope LifetimeScope { get; }

        public Func<IComponentContext, Workflow<TheoryContext>> ForEachTheory { get; set; }

        protected override async Task<Flow> ExecuteBody(SessionContext context)
        {
            var theories = context.Theories.ToLookup(p => p.Type);
            var processTheoryTasks =
                from theory in theories[Regular]
                select ProcessTheory(theory, theories[Template]);

            await Task.WhenAll(processTheoryTasks);
            return Flow.Continue;
        }

        private async Task ProcessTheory(Theory theory, IEnumerable<Theory> templates)
        {
            using var loggerScope = Logger.BeginScope("ProcessTheory", new { theory.Name });
            using var lifetimeScope = LifetimeScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(theory);
                builder.RegisterInstance(templates);
            });
            try
            {
                await ForEachTheory(lifetimeScope).ExecuteAsync(lifetimeScope.Resolve<TheoryContext>());
            }
            catch (Exception inner)
            {
                Logger.Scope().Exceptions.Push(inner);
            }
        }
    }
}