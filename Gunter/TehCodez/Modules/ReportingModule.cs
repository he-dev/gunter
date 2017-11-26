using Autofac;
using Gunter.Reporting;
using Gunter.Reporting.Modules;
using Module = Autofac.Module;

namespace Gunter.Modules
{
    internal class ReportingModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<Report>()
                .As<IReport>();

            //builder.RegisterType<Level>();
            builder.RegisterType<TestCase>();
            builder.RegisterType<DataSource>();
            builder.RegisterType<DataSummary>();
        }
    }
}
