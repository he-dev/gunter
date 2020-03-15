using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Gunter.Data;
using Gunter.Data.Configuration;
using Gunter.Workflows;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Steps;
using Reusable.OmniLog.Abstractions;

namespace Gunter.Workflow.Steps
{
    internal class ProcessTheories : Step<SessionContext>
    {
        public ProcessTheories(ILogger<ProcessTheories> logger, ILifetimeScope lifetimeScope)
        {
            Logger = logger;
            LifetimeScope = lifetimeScope;
        }

        private ILifetimeScope LifetimeScope { get; }

        private ILogger<ProcessTheories> Logger { get; }

        //public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount * 2;

        public Func<IComponentContext, Workflow<TheoryContext>> ForEachTheory { get; set; }

        public override async Task ExecuteAsync(SessionContext context)
        {
            var theories = context.Theories.ToLookup(p => p.Type);
            var theoryWorkflowTasks = theories[TheoryType.Regular].Select(theory => ProcessTheory(theory, theories[TheoryType.Template]));
            await Task.WhenAll(theoryWorkflowTasks);
            await ExecuteNextAsync(context);
        }

        private async Task ProcessTheory(Theory theory, IEnumerable<Theory> templates)
        {
            using var scope = LifetimeScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(theory);
                builder.RegisterInstance(templates);
            });

            //await ForEachTheory(scope).ExecuteAsync(default);

            await scope.Resolve<Workflow<TheoryContext>>().ExecuteAsync(default);
        }
    }
}