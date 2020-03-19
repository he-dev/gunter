using Autofac;
using Gunter.Data.Abstractions;
using Gunter.Data.Properties;
using Gunter.Services;
using Gunter.Services.Abstractions;
using Gunter.Services.DispatchMessage;
using Gunter.Services.Queries;
using Gunter.Services.Reporting;
using Gunter.Workflow.Data;
using Gunter.Workflow.Steps.SessionSteps;
using Gunter.Workflow.Steps.TestCaseSteps;
using Gunter.Workflow.Steps.TheorySteps;
using Microsoft.Extensions.Caching.Memory;
using Reusable.Extensions;
using Reusable.Flowingo.Steps;

namespace Gunter.DependencyInjection.Modules
{
    internal class WorkflowModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterGeneric(typeof(InstanceProperty<>));
            builder.RegisterGeneric(typeof(Workflow<>)).InstancePerDependency();

            builder.RegisterGeneric(typeof(InitializeLogger<>));

            // Session steps.
            builder.RegisterType<FindTheories>();
            builder.RegisterType<LoadTheories>();
            builder.RegisterType<ProcessTheories>();

            // Theory steps.
            builder.RegisterType<IgnoreTheoryWithDuplicateModelNames>();
            builder.RegisterType<ProcessTheory>();

            // Test-case steps.
            builder.RegisterType<GetData>();
            builder.RegisterType<FilterData>();
            builder.RegisterType<EvaluateData>();
            builder.RegisterType<ProcessMessages>();

            // Contexts

            builder.RegisterType<SessionContext>();
            builder.RegisterType<TheoryContext>();
            builder.RegisterType<TestContext>();

            // Services

            builder.RegisterType<DeserializeTheory>();
            builder.Register(_ => new MemoryCache(new MemoryCacheOptions())).As<IMemoryCache>().InstancePerLifetimeScope();

            builder.RegisterType<Format>().InstancePerDependency();
            builder.RegisterType<MergeProperty>().InstancePerDependency();
            builder.RegisterInstance(StaticProperty.For(() => ProgramInfo.Name));
            builder.RegisterInstance(StaticProperty.For(() => ProgramInfo.Version));
            builder.RegisterInstance(StaticProperty.For(() => ProgramInfo.FullName));
            builder.RegisterType<GetDataFromTableOrView>().As<IGetData>();
            builder.RegisterType<ThrowOperationCanceledException>().As<IDispatchMessage>();
            builder.RegisterType<DispatchEmail>().As<IDispatchMessage>().InstancePerDependency();
            
            builder.RegisterType<RenderHeading>();
            builder.RegisterType<RenderParagraph>();
            builder.RegisterType<RenderQuerySummary>();
            builder.RegisterType<RenderDataSummary>();
            builder.RegisterType<RenderTestSummary>();

            builder.Register(c => new Workflow<SessionContext>("session-workflow")
            {
                c.Resolve<InitializeLogger<SessionContext>>(),
                c.Resolve<FindTheories>(),
                c.Resolve<LoadTheories>(),
                c.Resolve<ProcessTheories>().Pipe(processTheories =>
                {
                    processTheories.ForEachTheory = theoryComponents => new Workflow<TheoryContext>("theory-workflow")
                    {
                        c.Resolve<IgnoreTheoryWithDuplicateModelNames>(),
                        theoryComponents.Resolve<ProcessTheory>().Pipe(processTheory =>
                        {
                            processTheory.ForEachTestCase = testCaseComponents => new Workflow<TestContext>("test-case-workflow")
                            {
                                testCaseComponents.Resolve<GetData>(),
                                testCaseComponents.Resolve<FilterData>(),
                                testCaseComponents.Resolve<EvaluateData>(),
                                testCaseComponents.Resolve<ProcessMessages>(),
                            };
                        })
                    };
                })
            });
        }
    }
}