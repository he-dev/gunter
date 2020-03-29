using Autofac;
using Gunter.Services;
using Gunter.Services.Abstractions;
using Gunter.Services.Merging;
using Gunter.Services.Queries;
using Gunter.Services.Reporting;
using Gunter.Services.Reporting.Tables;
using Gunter.Services.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Reusable.Extensions;

namespace Gunter.DependencyInjection.Modules
{
    internal class ServiceModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Renderers
            
            builder.RegisterType<RenderHeading>().AsImplementedInterfaces();
            builder.RegisterGeneric(typeof(RenderParagraph<>)).AsImplementedInterfaces();
            builder.RegisterType<RenderDataSummary>().AsImplementedInterfaces();
            builder.RegisterType<RenderQuerySummary>().AsImplementedInterfaces();
            builder.RegisterType<RenderTestSummary>().AsImplementedInterfaces();

            // Services
            
            builder.RegisterType<GetDataFromTableOrView>();
            builder.RegisterType<DeserializeTheory>();
            builder.Register(_ => new MemoryCache(new MemoryCacheOptions())).As<IMemoryCache>().InstancePerLifetimeScope();
            builder.RegisterType<SendEmailWithMailr>().InstancePerLifetimeScope();

            builder.RegisterType<TryGetPropertyValue>().As<ITryGetFormatValue>().InstancePerLifetimeScope();
            builder.RegisterType<MergeScalar>().As<IMergeScalar>().InstancePerLifetimeScope();
            builder.RegisterType<MergeCollection>().As<IMergeCollection>().InstancePerLifetimeScope();
            
            builder.RegisterType<GetDataFromTableOrView>().AsImplementedInterfaces();
            builder.RegisterType<ExecuteSendEmail>().AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterType<ExecuteHalt>().AsImplementedInterfaces().InstancePerDependency();
        }
    }
}
