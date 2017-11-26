using Autofac;

namespace Gunter.Modules
{
    internal class Data : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<Gunter.Data.SqlClient.TableOrView>();

        }
    }
}