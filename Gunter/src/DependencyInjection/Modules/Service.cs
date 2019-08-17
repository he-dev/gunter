using System;
using System.Collections.Immutable;
using System.Configuration;
using System.Net.Http;
using Autofac;
using Gunter.Data;
using Gunter.Reporting;
using Gunter.Services;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Reusable;
using Reusable.Commander;
using Reusable.Commander.DependencyInjection;
using Reusable.Data;
using Reusable.Extensions;
using Reusable.IO;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.Quickey;
using Reusable.Translucent;
using Reusable.Translucent.Controllers;
using Reusable.Translucent.Middleware;
using Reusable.Utilities.Autofac;
using Reusable.Utilities.JsonNet;
using Reusable.Utilities.JsonNet.Converters;
using Reusable.Utilities.JsonNet.DependencyInjection;
using IServiceProvider = System.IServiceProvider;
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
                .RegisterType<MiddlewareBuilderWithAutofac>();

            builder
                .RegisterType<AutofacServiceProvider>()
                .As<IServiceProvider>();

            builder
                .RegisterType<ResourceRepository<GunterResourceSetup>>()
                .As<IResourceRepository>();

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
                .RegisterType<RuntimePropertyNameValidator>()
                .As<IRuntimePropertyNameValidator>();

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

    internal class GunterResourceSetup
    {
        public void ConfigureServices(IResourceControllerBuilder controller)
        {
            controller.AddPhysicalFiles();
            controller.AddJsonFile(ProgramInfo.CurrentDirectory, "appsettings.json");
            controller.AddHttp(new HttpClient(new HttpClientHandler { UseProxy = false })
            {
                BaseAddress = new Uri(ProgramInfo.Configuration["mailr:BaseUri"])
            }, ImmutableContainer.Empty.UpdateItem(ResourceController.Tags, tags => tags.Add("Mailr")));
        }

        public void Configure(IResourceRepositoryBuilder repository)
        {
            repository
                .UseTelemetry(repository.ServiceProvider.Resolve<ILogger<TelemetryMiddleware>>())
                .UseEnvironmentVariables()
                .UseMiddleware<ResourceExistsValidationMiddleware>()
                .UseMiddleware<SettingFormatValidationMiddleware>();
        }
    }
}