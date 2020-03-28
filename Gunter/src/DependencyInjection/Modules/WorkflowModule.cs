using Autofac;
using Gunter.Workflow.Data;
using Gunter.Workflow.Steps.SessionSteps;
using Gunter.Workflow.Steps.TestCaseSteps;
using Gunter.Workflow.Steps.TheorySteps;
using Reusable.Extensions;
using Reusable.Flowingo.Steps;

namespace Gunter.DependencyInjection.Modules
{
    internal class WorkflowModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterGeneric(typeof(Workflow<>)).InstancePerDependency();
            builder.RegisterGeneric(typeof(InitializeLogger<>));

            // Session steps.

            builder.RegisterType<FindTheories>();
            builder.RegisterType<LoadTheories>();
            builder.RegisterType<ProcessTheories>();

            // Theory steps.

            builder.RegisterType<IgnoreTheoryWithDuplicateModels>();
            builder.RegisterType<ProcessTheory>();

            // Test-case steps.
            builder.RegisterType<GetData>();
            builder.RegisterType<FilterData>();
            builder.RegisterType<EvaluateData>();
            builder.RegisterType<ProcessTasks>();

            // Contexts

            builder.RegisterType<SessionContext>().InstancePerLifetimeScope();
            builder.RegisterType<TheoryContext>().InstancePerLifetimeScope();
            builder.RegisterType<TestContext>().InstancePerLifetimeScope();

            builder.Register(c => new Workflow<SessionContext>("session-workflow")
            {
                c.Resolve<InitializeLogger<SessionContext>>(),
                c.Resolve<FindTheories>(),
                c.Resolve<LoadTheories>(),
                c.Resolve<ProcessTheories>().Pipe(processTheories =>
                {
                    processTheories.ForEachTheory = theoryComponents => new Workflow<TheoryContext>("theory-workflow")
                    {
                        theoryComponents.Resolve<IgnoreTheoryWithDuplicateModels>(),
                        theoryComponents.Resolve<ProcessTheory>().Pipe(processTheory =>
                        {
                            processTheory.ForEachTestCase = testCaseComponents => new Workflow<TestContext>("test-case-workflow")
                            {
                                testCaseComponents.Resolve<GetData>(),
                                testCaseComponents.Resolve<FilterData>(),
                                testCaseComponents.Resolve<EvaluateData>(),
                                testCaseComponents.Resolve<ProcessTasks>(),
                            };
                        })
                    };
                })
            });
        }
    }
}