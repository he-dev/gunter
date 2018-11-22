using Autofac;

namespace Gunter.ComponentSetup
{
    internal class Mailr : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<Messaging.Mailr>();
        }
    }
}