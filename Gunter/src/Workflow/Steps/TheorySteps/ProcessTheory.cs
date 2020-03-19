using System;
using System.Linq;
using System.Linq.Custom;
using System.Threading.Tasks;
using Autofac;
using Gunter.Data.Abstractions;
using Gunter.Data.Configuration;
using Gunter.Data.Configuration.Sections;
using Gunter.Data.Properties;
using Gunter.Services;
using Gunter.Workflow.Data;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Data;
using Reusable.Flowingo.Steps;
using Reusable.Utilities.Autofac;

namespace Gunter.Workflow.Steps.TheorySteps
{
    internal class ProcessTheory : Step<TheoryContext>
    {
        public ProcessTheory(MergeProperty mergeProperty, ILifetimeScope lifetimeScope, Theory theory)
        {
            MergeProperty = mergeProperty;
            LifetimeScope = lifetimeScope;
            Theory = theory;
        }

        private MergeProperty MergeProperty { get; }

        private ILifetimeScope LifetimeScope { get; }

        private Theory Theory { get; }
        
        public Func<IComponentContext, Workflow<TestContext>> ForEachTestCase { get; set; }

        protected override async Task<Flow> ExecuteBody(TheoryContext context)
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

            return Flow.Continue;
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

                var properties = Theory.Resolve(x => x.Properties).With(MergeProperty).Flatten();
                builder.RegisterEnumerable(properties, r => r.As<IProperty>());
            });

            await ForEachTestCase(scope).ExecuteAsync(scope.Resolve<TestContext>());
        }
    }
}