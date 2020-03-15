using Autofac;
using Gunter.Data;
using Gunter.Queries;
using Gunter.Services;
using Gunter.Services.Abstractions;
using Gunter.Services.Reporting;
using Gunter.Workflow.Steps;
using Gunter.Workflows;
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
            builder.RegisterType<FindTheories>();
            builder.RegisterType<LoadTheories>();
            builder.RegisterType<ProcessTheories>();
            builder.RegisterType<ProcessTheory>();
            builder.RegisterType<GetData>();
            builder.RegisterType<FilterData>();
            builder.RegisterType<EvaluateData>();
            builder.RegisterType<SendMessages>();
            
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

            builder.Register(c => new Workflow<SessionContext>().Pipe(sessionWorkflow =>
            {
                sessionWorkflow.Add(c.Resolve<FindTheories>());
                sessionWorkflow.Add(c.Resolve<LoadTheories>());
                sessionWorkflow.Add(c.Resolve<ProcessTheories>().Pipe(processTheories =>
                {
                    processTheories.ForEachTheory = theoryComponents => theoryComponents.Resolve<Workflow<TheoryContext>>().Pipe(theoryWorkflow =>
                    {
                        theoryWorkflow.Add(theoryComponents.Resolve<ProcessTheory>().Pipe(processTheory =>
                        {
                            processTheory.ForEachTestCase = testCaseComponents => testCaseComponents.Resolve<Workflow<TestContext>>().Pipe(testWorkflow =>
                            {
                                testWorkflow.Add(testCaseComponents.Resolve<GetData>());
                                testWorkflow.Add(testCaseComponents.Resolve<FilterData>());
                                testWorkflow.Add(testCaseComponents.Resolve<EvaluateData>());
                                testWorkflow.Add(testCaseComponents.Resolve<SendMessages>());
                            });
                        }));
                    });
                }));
            }));
        }
    }
}