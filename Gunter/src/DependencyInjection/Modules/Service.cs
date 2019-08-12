using System.Collections.Immutable;
using System.Configuration;
using System.IO;
using Autofac;
using Gunter.Data;
using Gunter.Reporting;
using Gunter.Services;
using Reusable.Commander;
using Reusable.Commander.DependencyInjection;
using Reusable.Data;
using Reusable.IOnymous;
using Reusable.IOnymous.Config;
using Reusable.IOnymous.Http;
using Reusable.Quickey;
using Reusable.Utilities.JsonNet.DependencyInjection;
using Module = Autofac.Module;

namespace Gunter.DependencyInjection.Modules
{
    internal class Service : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            
            builder
                .RegisterType<PhysicalDirectoryTree>()
                .As<IDirectoryTree>();

            builder
                .Register(c =>
                {
                    var appSettings = new JsonProvider(ProgramInfo.CurrentDirectory, "appsettings.json");
                    return
                        CompositeProvider
                            .Empty
                            .Add(appSettings)
                            .Add(new PhysicalFileProvider().DecorateWith(EnvironmentVariableProvider.Factory()))
                            .Add(new HttpProvider(appSettings.ReadSetting(MailrConfig.BaseUri), ImmutableContainer.Empty.SetName(ResourceProvider.CreateTag("Mailr"))));
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

            var commands =
                ImmutableList<CommandModule>
                    .Empty
                    .Add<Commands.Run>()
                    .Add<Commands.Send>()
                    .Add<Commands.Halt>();

            builder
                .RegisterModule(new CommanderModule(commands));
        }
    }
}