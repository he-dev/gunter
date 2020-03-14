using Autofac;
using Gunter.Data.Configuration;
using Gunter.Data.Configuration.Abstractions;
using Gunter.Data.Reporting;
using Gunter.Reporting;

namespace Gunter.DependencyInjection.Modules
{
    internal class Reporting : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<TestInfo>();
            builder.RegisterType<QueryInfo>();
            builder.RegisterType<DataInfo>();
            builder.RegisterType<TestInfo>().AsSelf();
            builder.RegisterType<Report>().AsSelf();
            builder.RegisterType<Report>().As<IReport>();//.AsSelf();
        }
    }
}
