using Autofac;
using Gunter.Data.SqlClient;
using Reusable.Logging;
using Reusable.Logging.Loggex;

namespace Gunter.AutofacModules
{
    internal class DataModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<Data.SqlClient.TableOrView>()
                .WithParameter(new TypedParameter(typeof(ILogger), Logger.Create<TableOrView>()));

        }
    }
}