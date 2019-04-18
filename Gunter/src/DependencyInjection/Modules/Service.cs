using System.Configuration;
using Autofac;
using Gunter.Data;
using Gunter.Services;
using Reusable.Commander;
using Reusable.IOnymous;
using Reusable.SmartConfig;
using Reusable.Utilities.JsonNet.DependencyInjection;
using Configuration = Reusable.SmartConfig.Configuration;

namespace Gunter.DependencyInjection.Modules
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
                .RegisterInstance(new Configuration(new AppSettingProvider()))
                .As<IConfiguration>();

            builder
                .RegisterInstance(new Configuration<IProgramConfig>(new AppSettingProvider()))
                .As<IConfiguration<IProgramConfig>>();
            
            builder
                .RegisterInstance(new Configuration<IMailrConfig>(new AppSettingProvider()))
                .As<IConfiguration<IMailrConfig>>();
            
            builder
                .Register(c =>
                {
                    var programConfig = c.Resolve<IConfiguration<IMailrConfig>>();
                    return new CompositeProvider(new IResourceProvider[]
                    {
                        new PhysicalFileProvider().DecorateWith(EnvironmentVariableProvider.Factory()),
                        new HttpProvider(programConfig.GetItem(x => x.BaseUri))
                    }, Metadata.Empty.AllowRelativeUri(true));
                })
                .As<IResourceProvider>();                                  

            builder
                .RegisterType<TestFileSerializer>()
                .As<ITestFileSerializer>();

            builder
                .RegisterInstance(RuntimeVariables.Enumerate());
            
            builder
                .RegisterModule<JsonContractResolverModule>();

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
                            .Add<Commands.Send>()
                            .Add<Commands.Halt>()
                    )
                );
        }
    }
}