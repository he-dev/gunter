using Autofac;

namespace Gunter.Modules
{
    internal class DataModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<Data.SqlClient.TableOrView>();

        }
    }
}