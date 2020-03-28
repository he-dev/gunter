using Autofac;
using Gunter.Services.Tasks;

namespace Gunter.DependencyInjection.Modules
{
    internal class Mailr : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ExecuteSendEmail>();
        }
    }
}