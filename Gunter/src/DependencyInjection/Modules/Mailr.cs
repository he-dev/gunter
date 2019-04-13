using Autofac;

namespace Gunter.DependencyInjection.Modules
{
    internal class Mailr : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<Services.Messengers.Mailr>();
        }
    }
}