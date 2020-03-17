using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Gunter.Data;
using Gunter.Data.Configuration;
using Gunter.Workflow.Data;
using Reusable.Extensions;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Data;
using Reusable.Flowingo.Steps;
using Reusable.OmniLog.Abstractions;

namespace Gunter.Workflow.Steps
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
            using var scope = LifetimeScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(theory);
                builder.RegisterInstance(templates);
            });

            await ForEachTheory(scope).ExecuteAsync(scope.Resolve<TheoryContext>());
        }
    }
}