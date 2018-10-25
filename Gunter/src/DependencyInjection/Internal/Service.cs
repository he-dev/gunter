using Autofac;
using Gunter.DependencyInjection.Helpers;
using Gunter.Services;
using MailrNET;
using Newtonsoft.Json.Serialization;
using Reusable.IO;

namespace Gunter.DependencyInjection.Internal
{
    internal class Service : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            //builder
            //    .RegisterType<RuntimeFormatterFactory>()
            //    .As<IRuntimeFormatterFactory>();

            builder
                .Register(c =>
                {
                    var context = c.Resolve<IComponentContext>();
                    return new AutofacContractResolver(context);
                }).SingleInstance()
                .As<IContractResolver>();

            builder
                .RegisterType<FileSystem>()
                .As<IFileSystem>();

            builder
                .RegisterType<VariableNameValidator>()
                .As<IVariableNameValidator>();

            builder
                .RegisterType<TestLoader>()
                .As<ITestLoader>();

            builder
                .RegisterType<TestComposer>()
                .AsSelf();

            builder
                .RegisterType<TestRunner>()
                .As<ITestRunner>();

            builder
                .RegisterType<RuntimeFormatter>()
                .AsSelf();

            builder
                .Register(c =>
                {
                    var program = c.Resolve<Gunter.Program>();
                    return MailrClient.Create(
                        program.MailrBaseUri, 
                        program.Product, 
                        program.Version, 
                        $".{program.Environment}"
                    );
                })
                .InstancePerLifetimeScope();
        }
    }
}