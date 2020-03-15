using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Gunter.Data;
using Gunter.Data.Configuration;
using Gunter.Workflows;
using Microsoft.Extensions.Caching.Memory;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Steps;

namespace Gunter.Workflow.Steps
{
    internal class ProcessTheory : Step<TheoryContext>
    {
        public ProcessTheory(ILifetimeScope lifetimeScope, Theory theory)
        {
            LifetimeScope = lifetimeScope;
            Theory = theory;
        }

        private ILifetimeScope LifetimeScope { get; }

        private Theory Theory { get; }

        public Func<IComponentContext, Workflow<TestContext>> ForEachTestCase { get; set; }

        public override async Task ExecuteAsync(TheoryContext context)
        {
            var testCases =
                from testCase in Theory.Tests
                from queryName in testCase.QueryNames
                join query in Theory.Queries on queryName equals query.Name
                select (testCase, query);

            foreach (var (testCase, query) in testCases)
            {
                using var scope = LifetimeScope.BeginLifetimeScope(builder =>
                {
                    builder.RegisterInstance(new MemoryCache(new MemoryCacheOptions()));
                    builder.RegisterInstance(testCase);
                    builder.RegisterInstance(query).As<IQuery>();
                    builder.Register(c => c.Resolve<InstanceProperty<TestCase>.Factory>()(x => x.Level)).As<IProperty>();
                    builder.Register(c => c.Resolve<InstanceProperty<TestCase>.Factory>()(x => x.Message)).As<IProperty>();
                    builder.Register(c => c.Resolve<InstanceProperty<TestContext>.Factory>()(x => x.GetDataElapsed)).As<IProperty>();
                });

                try
                {
                    await ForEachTestCase(scope).ExecuteAsync(scope.Resolve<TestContext>());
                }
                catch (OperationCanceledException)
                {
                    // log
                }
            }

            await ExecuteNextAsync(context);
        }
    }
}