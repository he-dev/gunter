using Autofac;
using Gunter.Services.DispatchMessage;

namespace Gunter.DependencyInjection.Modules
{
    internal class Mailr : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DispatchEmail>();
        }
    }
}