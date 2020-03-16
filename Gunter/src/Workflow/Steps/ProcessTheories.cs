using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Gunter.Data;
using Gunter.Data.Configuration;
using Gunter.Workflow.Data;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Steps;
using Reusable.OmniLog.Abstractions;

namespace Gunter.Workflow.Steps
{
    using static TheoryType;

    internal class ProcessTheories : Step<SessionContext>
    {
        public ProcessTheories(ILogger<ProcessTheories> logger, ILifetimeScope lifetimeScope) : base(logger)
        {
            Logger = logger;
            LifetimeScope = lifetimeScope;
        }

        private ILifetimeScope LifetimeScope { get; }

        private ILogger<ProcessTheories> Logger { get; }

        //public Func<IComponentContext, Workflow<TheoryContext>> ForEachTheory { get; set; }

        protected override async Task<bool> ExecuteBody(SessionContext context)
        {
            var theories = context.Theories.ToLookup(p => p.Type);
            var processTheoryTasks =
                from theory in theories[Regular]
                select ProcessTheory(theory, theories[Template]);

            await Task.WhenAll(processTheoryTasks);
            return true;
        }

        private async Task ProcessTheory(Theory theory, IEnumerable<Theory> templates)
        {
            using var scope = LifetimeScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(theory);
                builder.RegisterInstance(templates);
            });

            //await ForEachTheory(scope).ExecuteAsync(scope.Resolve<TheoryContext>());
            await scope.Resolve<Workflow<TheoryContext>>().ExecuteAsync(scope.Resolve<TheoryContext>());
        }
    }
}