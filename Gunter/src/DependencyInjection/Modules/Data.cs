using Autofac;
using Gunter.Data.Configuration;

namespace Gunter.DependencyInjection.Modules
{
    internal class Data : Autofac.Module
    {
        protected override void Load(Autofac.ContainerBuilder builder)
        {
            builder.RegisterType<Theory>();
            builder.RegisterType<TestCase>();
            builder.RegisterType<Gunter.Data.ConstantPropertyCollection>();
            builder.RegisterType<TableOrView>();
        }
    }
}