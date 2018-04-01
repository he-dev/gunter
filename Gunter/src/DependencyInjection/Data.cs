using Autofac;
using Gunter.Data;
using Gunter.Reporting;
using Module = Autofac.Module;

namespace Gunter.DependencyInjection
{
    internal class Data : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<Gunter.Data.SqlClient.TableOrView>();

            builder
                .RegisterType<TestCase>()
                .AsSelf();

            builder
                .RegisterType<Report>();
        }
    }
}