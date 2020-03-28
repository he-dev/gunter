using Autofac;
using Gunter.Data.Configuration.Abstractions;
using Gunter.Data.Configuration.Reports;
using Gunter.Data.Configuration.Reports.CustomSections;

namespace Gunter.DependencyInjection.Modules
{
    internal class Reporting : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<TestSummary>();
            builder.RegisterType<QuerySummary>();
            builder.RegisterType<DataSummary>();
            builder.RegisterType<TestSummary>().AsSelf();
            builder.RegisterType<Custom>().AsSelf();
            builder.RegisterType<Custom>().As<IReport>();//.AsSelf();
        }
    }
}
