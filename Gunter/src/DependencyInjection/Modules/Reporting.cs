using Autofac;
using Gunter.Reporting;

namespace Gunter.DependencyInjection.Modules
{
    internal class Reporting : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<Gunter.Reporting.Modules.TestCase>();
            builder.RegisterType<Gunter.Reporting.Modules.DataSource>();
            builder.RegisterType<Gunter.Reporting.Modules.DataSummary>();
            builder.RegisterType<Gunter.Reporting.Modules.TestCase>().AsSelf();
            builder.RegisterType<Gunter.Reporting.Report>().AsSelf();
            builder.RegisterType<Gunter.Reporting.Report>().As<IReport>();//.AsSelf();
        }
    }
}
