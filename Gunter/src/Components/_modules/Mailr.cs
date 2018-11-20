using Autofac;

namespace Gunter.Components
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