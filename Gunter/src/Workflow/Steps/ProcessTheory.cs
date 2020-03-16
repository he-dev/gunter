using System;
using System.Linq;
using System.Linq.Custom;
using System.Threading.Tasks;
using Autofac;
using Gunter.Data;
using Gunter.Data.Configuration;
using Gunter.Services;
using Gunter.Workflow.Data;
using Microsoft.Extensions.Caching.Memory;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Steps;
using Reusable.OmniLog.Abstractions;
using Reusable.Utilities.Autofac;

namespace Gunter.Workflow.Steps
{
    internal class ProcessTheory : Step<TheoryContext>
    {
        public ProcessTheory(ILogger<ProcessTheory> logger, Merge merge, ILifetimeScope lifetimeScope, Theory theory):base(logger)
        {
            Merge = merge;
            LifetimeScope = lifetimeScope;
            Theory = theory;
        }

        private Merge Merge { get; }

        private ILifetimeScope LifetimeScope { get; }

        private Theory Theory { get; }

        public Func<IComponentContext, Workflow<TestContext>> ForEachTestCase { get; set; }

        protected override async Task<bool> ExecuteBody(TheoryContext context)
        {
            var testCases =
                from testCase in Theory.Tests
                from queryName in testCase.QueryNames
                join query in Theory.Queries on queryName equals query.Name
                select (testCase, query);

            try
            {
                foreach (var (testCase, query) in testCases)
                {
                    await ProcessTestCase(testCase, query);
                }
            }
            catch (OperationCanceledException)
            {
                // ...
            }

            return true;
        }

        private async Task ProcessTestCase(TestCase testCase, IQuery query)
        {
            using var scope = LifetimeScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(testCase);
                builder.RegisterInstance(query);
                
                builder.Register(c => c.Resolve<InstanceProperty<Theory>.Factory>()(x => x.FileName)).As<IProperty>();
                builder.Register(c => c.Resolve<InstanceProperty<Theory>.Factory>()(x => x.Name)).As<IProperty>();
                builder.Register(c => c.Resolve<InstanceProperty<TestCase>.Factory>()(x => x.Level)).As<IProperty>();
                builder.Register(c => c.Resolve<InstanceProperty<TestCase>.Factory>()(x => x.Message)).As<IProperty>();
                builder.Register(c => c.Resolve<InstanceProperty<TestContext>.Factory>()(x => x.GetDataElapsed)).As<IProperty>();
                
                var properties = Theory.Resolve(x => x.Properties).With(Merge).Flatten();
                builder.RegisterEnumerable(properties, r => r.As<IProperty>());
            });

            //await ForEachTestCase(scope).ExecuteAsync(scope.Resolve<TestContext>());
            await scope.Resolve<Workflow<TestContext>>().ExecuteAsync(scope.Resolve<TestContext>());
        }
    }
}