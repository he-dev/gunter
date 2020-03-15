using Autofac;
using Gunter.Data.Configuration;
using Gunter.Data.Configuration.Abstractions;
using Gunter.Data.Configuration.Reporting;
using Gunter.Reporting;

namespace Gunter.DependencyInjection.Modules
{
    internal class Reporting : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<TestInfo>();
            builder.RegisterType<QueryInfo>();
            builder.RegisterType<DataSummary>();
            builder.RegisterType<TestInfo>().AsSelf();
            builder.RegisterType<Report>().AsSelf();
            builder.RegisterType<Report>().As<IReport>();//.AsSelf();
        }
    }
}
