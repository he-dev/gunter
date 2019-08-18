using Autofac;
using Gunter.Reporting;
using Gunter.Reporting.Modules.Tabular;

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
            builder.RegisterType<Gunter.Reporting.Report>().AsSelf();
            builder.RegisterType<Gunter.Reporting.Report>().As<IReport>();//.AsSelf();
        }
    }
}
