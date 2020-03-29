using Autofac;
using Gunter.Data.Configuration;
using Gunter.Data.Configuration.Abstractions;
using Gunter.Data.Configuration.Queries;
using Gunter.Data.Configuration.Reports;
using Gunter.Data.Configuration.Reports.CustomSections;
using Gunter.Data.Configuration.Sections;
using Gunter.Data.Configuration.Tasks;
using Gunter.Data.Properties;
using Gunter.Services.Tasks;

namespace Gunter.DependencyInjection.Modules
{
    internal class DataModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Queries

            builder.RegisterType<TableOrView>();

            // Sections

            builder.RegisterType<TestCase>();
            builder.RegisterType<PropertyCollection>();

            // Reports

            builder.RegisterType<Custom>().AsSelf();
            builder.RegisterType<Custom>().As<IReport>();

            // Tasks

            builder.RegisterType<Halt>();
            builder.RegisterType<SendEmail>();
            
            // Other

            builder.RegisterType<Theory>();
            
            builder.RegisterGeneric(typeof(InstanceProperty<>)).InstancePerDependency();
            builder.RegisterInstance(StaticProperty.For(() => ProgramInfo.Name));
            builder.RegisterInstance(StaticProperty.For(() => ProgramInfo.Version));
            builder.RegisterInstance(StaticProperty.For(() => ProgramInfo.FullName));
        }
    }
}