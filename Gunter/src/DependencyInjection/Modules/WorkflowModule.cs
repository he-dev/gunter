using Autofac;
using Gunter.Data;
using Gunter.Queries;
using Gunter.Services;
using Gunter.Services.Abstractions;
using Gunter.Services.Reporting;
using Gunter.Workflow.Data;
using Gunter.Workflow.Steps;
using Microsoft.Extensions.Caching.Memory;
using Reusable.Extensions;
using Reusable.Flowingo.Steps;
using Reusable.OmniLog.Abstractions;

namespace Gunter.DependencyInjection.Modules
{
    internal class WorkflowModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterGeneric(typeof(InstanceProperty<>));
            builder.RegisterGeneric(typeof(Workflow<>)).InstancePerDependency();
            
            // Session steps.
            builder.RegisterType<FindTheories>().AsImplementedInterfaces();
            builder.RegisterType<LoadTheories>().AsImplementedInterfaces();
            builder.RegisterType<ProcessTheories>().AsImplementedInterfaces();
            
            // Theory steps.
            builder.RegisterType<ProcessTheory>().AsImplementedInterfaces();
            
            // Test-case steps.
            builder.RegisterType<GetData>().AsImplementedInterfaces();
            builder.RegisterType<FilterData>().AsImplementedInterfaces();
            builder.RegisterType<EvaluateData>().AsImplementedInterfaces();
            builder.RegisterType<ProcessMessages>().AsImplementedInterfaces();
            
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

            // builder.Register(c => new Workflow<SessionContext>(c.Resolve<ILogger<Workflow<SessionContext>>>()).Pipe(sessionWorkflow =>
            // {
            //     sessionWorkflow.Add(c.Resolve<FindTheories>());
            //     sessionWorkflow.Add(c.Resolve<LoadTheories>());
            //     sessionWorkflow.Add(c.Resolve<ProcessTheories>().Pipe(processTheories =>
            //     {
            //         processTheories.ForEachTheory = theoryComponents => new Workflow<TheoryContext>(c.Resolve<ILogger<Workflow<TheoryContext>>>())
            //         {
            //             theoryComponents.Resolve<ProcessTheory>().Pipe(processTheory =>
            //             {
            //                 processTheory.ForEachTestCase = testCaseComponents => new Workflow<TestContext>(c.Resolve<ILogger<Workflow<TestContext>>>())
            //                 {
            //                     testCaseComponents.Resolve<GetData>(),
            //                     testCaseComponents.Resolve<FilterData>(),
            //                     testCaseComponents.Resolve<EvaluateData>(),
            //                     testCaseComponents.Resolve<ProcessMessages>(),
            //                 };
            //             })
            //         };
            //     }));
            // }));
        }
    }
}