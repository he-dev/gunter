using Autofac;

namespace Gunter.Components
{
    internal class Data : Autofac.Module
    {
        protected override void Load(Autofac.ContainerBuilder builder)
        {
            builder
                .RegisterType<Gunter.Data.TestBundle>();

            builder
                .RegisterType<Gunter.Data.SqlClient.TableOrView>();

            builder
                .RegisterType<Gunter.Data.TestCase>();
            
            builder
                .RegisterType<Gunter.Data.VariableCollection>();
        }
    }
}