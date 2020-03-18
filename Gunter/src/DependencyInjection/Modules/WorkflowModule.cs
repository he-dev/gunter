using System.Collections.Generic;
using Autofac;
using Gunter.Data;
using Gunter.Data.Configuration;
using Gunter.Queries;
using Gunter.Services;
using Gunter.Services.Abstractions;
using Gunter.Services.Reporting;
using Gunter.Workflow.Data;
using Gunter.Workflow.Steps;
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
            builder.RegisterType<Merge>().InstancePerDependency();
            builder.RegisterInstance(new StaticProperty(() => ProgramInfo.FullName));
            builder.RegisterInstance(new StaticProperty(() => ProgramInfo.Version));
            builder.RegisterType<GetDataTableOrView>().As<IGetData>();
            builder.RegisterType<DispatchEmail>().As<IDispatchMessage>().InstancePerDependency();
            builder.RegisterType<RenderDataSummary>();
            builder.RegisterType<RenderQuerySummary>();

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
                                testCaseComponents.Resolve<ProcessMessages>().Pipe(x =>
                                {
                                    x.ServiceMappings = new List<IServiceMapping>
                                    {
                                        Handle<Email>.With<DispatchEmail>(),
                                        Handle<Halt>.With<ThrowOperationCanceledException>()
                                    };
                                }),
                            };
                        })
                    };
                })
            });
        }
    }
}