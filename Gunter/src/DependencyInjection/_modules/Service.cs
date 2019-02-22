using System.Configuration;
using Autofac;
using Gunter.Data;
using Gunter.Services;
using Newtonsoft.Json.Serialization;
using Reusable.Commander;
using Reusable.IOnymous;
using Reusable.SmartConfig;
using Configuration = Reusable.SmartConfig.Configuration;

namespace Gunter.DependencyInjection
{
    internal class Service : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            //builder
            //    .RegisterType<RuntimeFormatterFactory>()
            //    .As<IRuntimeFormatterFactory>();

            builder
                .RegisterType<PhysicalDirectoryTree>()
                .As<IDirectoryTree>();

            //builder
            //    .RegisterInstance(new PhysicalFileProvider().DecorateWith(EnvironmentVariableProvider.Factory()))
            //    .As<IResourceProvider>();

            //builder
            //    .RegisterInstance(new AppSettingProvider(new UriStringToSettingIdentifierConverter()))
            //    .As<IResourceProvider>();

            //            builder
            //                .RegisterType<CompositeResourceProvider>()
            //                .As<IFirstResourceProvider>();

            builder
                .RegisterInstance(new Configuration(new CompositeProvider(new IResourceProvider[]
                {
                    new AppSettingProvider(new UriStringToSettingIdentifierConverter()).DecorateWith(SettingNameProvider.Factory()),
                })))
                //.RegisterType<Configuration>()
                .As<IConfiguration>();

            builder
                .RegisterInstance(new CompositeProvider(new IResourceProvider[]
                {
                    new PhysicalFileProvider().DecorateWith(EnvironmentVariableProvider.Factory()),
                    new HttpProvider(ConfigurationManager.AppSettings["mailr:BaseUri"])
                }, ResourceMetadata.Empty.AllowRelativeUri(true)))
                .As<IResourceProvider>();

            builder
                .RegisterType<TestFileSerializer>()
                .As<ITestFileSerializer>();

            builder
                .RegisterInstance(RuntimeValue.Enumerate());

            builder
                .Register(c =>
                {
                    var context = c.Resolve<IComponentContext>();
                    return new AutofacContractResolver(context);
                })
                .SingleInstance()
                .As<IContractResolver>();

            builder
                .RegisterType<VariableNameValidator>()
                .As<IVariableNameValidator>();

            builder
                .RegisterType<TestLoader>()
                .As<ITestLoader>();

            builder
                .RegisterType<TestComposer>()
                .As<ITestComposer>();

            builder
                .RegisterType<TestRunner>()
                .As<ITestRunner>();

            builder
                .RegisterType<RuntimeVariableDictionaryFactory>()
                .AsSelf();

            builder
                .RegisterModule(
                    new CommanderModule(commands =>
                        commands
                            .Add<Commands.Run>()
                    )
                );
        }
    }
}