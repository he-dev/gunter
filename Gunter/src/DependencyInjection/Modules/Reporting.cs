using Autofac;
using Gunter.Data.Configuration.Abstractions;
using Gunter.Data.Configuration.Reporting;
using Gunter.Data.Configuration.Sections;

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
            builder.RegisterType<Report>().AsSelf();
            builder.RegisterType<Report>().As<IReport>();//.AsSelf();
        }
    }
}
