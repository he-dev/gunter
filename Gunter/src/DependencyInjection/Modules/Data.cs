using Autofac;
using Gunter.Data.Configuration;
using Gunter.Data.Configuration.Queries;
using Gunter.Data.Configuration.Sections;

namespace Gunter.DependencyInjection.Modules
{
    internal class Data : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<Theory>();
            builder.RegisterType<TestCase>();
            builder.RegisterType<PropertyCollection>();
            builder.RegisterType<TableOrView>();
        }
    }
}