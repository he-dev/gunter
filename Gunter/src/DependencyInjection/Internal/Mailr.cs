using Autofac;

namespace Gunter.DependencyInjection.Internal
{
    internal class Mailr : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
//            builder
//                .RegisterType<LevelFactory>()
//                .As<IModuleFactory>();
//
//            builder
//                .RegisterType<GreetingFactory>()
//                .As<IModuleFactory>();
//
//            builder
//                .RegisterType<TableFactory>()
//                .As<IModuleFactory>();
//
//            builder
//                .RegisterType<SignatureFactory>()
//                .As<IModuleFactory>();

            builder
                .RegisterType<Messaging.Mailr>();
        }
    }
}