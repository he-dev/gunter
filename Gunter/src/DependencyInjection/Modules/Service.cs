using System;
using System.Collections.Immutable;
using System.Configuration;
using System.Net.Http;
using Autofac;
using Gunter.Data;
using Gunter.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Reusable.Commander;
using Reusable.Commander.DependencyInjection;
using Reusable.Data;
using Reusable.Extensions;
using Reusable.IOnymous;
using Reusable.IOnymous.Config;
using Reusable.IOnymous.Http;
using Reusable.OmniLog;
using Reusable.Quickey;
using Reusable.Utilities.JsonNet;
using Reusable.Utilities.JsonNet.Converters;
using Reusable.Utilities.JsonNet.DependencyInjection;

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
                    var httpProvider = new HttpProvider(new HttpClient(new HttpClientHandler { UseProxy = false })
                    {
                        BaseAddress = new Uri(appSettings.ReadSetting(MailrConfig.BaseUri))
                    }, ImmutableContainer.Empty.SetName(ResourceProvider.CreateTag("Mailr")));
                    return
                        CompositeProvider
                            .Empty
                            .Add(appSettings)
                            .Add(new PhysicalFileProvider().DecorateWith(EnvironmentVariableProvider.Factory()))
                            .Add(httpProvider);
                })
                .As<IResourceProvider>();

//            builder
//                .RegisterType<TestFileSerializer>()
//                .As<ITestFileSerializer>();

            builder
                .Register(ctx =>
                {
                    return new PrettyJsonSerializer(ctx.Resolve<IContractResolver>(), serializer =>
                    {
                        serializer.Converters.Add(new LambdaJsonConverter<LogLevel>
                        {
                            ReadJsonCallback = LogLevel.FromName
                        });
                        serializer.Converters.Add(new JsonStringConverter());
                        serializer.DefaultValueHandling = DefaultValueHandling.Populate;
                    });
                })
                .As<IPrettyJsonSerializer>();

            builder
                .RegisterInstance(RuntimeProperty.BuiltIn.Enumerate());

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
                .RegisterType<RuntimePropertyProvider>()
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