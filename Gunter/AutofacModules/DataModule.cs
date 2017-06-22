using Autofac;
using Reusable.Logging;

namespace Gunter.AutofacModules
{
    internal class DataModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<Data.SqlClient.TableOrView>()
                .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(Data.SqlClient.TableOrView))));

        }
    }
}