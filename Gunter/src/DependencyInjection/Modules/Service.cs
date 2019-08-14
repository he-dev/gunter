using System;
using System.Collections.Immutable;
using System.Configuration;
using System.Net.Http;
using Autofac;
using Gunter.Data;
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
using Reusable.IOnymous;
using Reusable.IOnymous.Config;
using Reusable.IOnymous.Controllers;
using Reusable.IOnymous.Http;
using Reusable.IOnymous.Middleware;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
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
                .RegisterType<MiddlewareBuilderWithAutofac>();

            builder
                .Register(ctx =>
                {
                    var middlewareBuilder = ctx.Resolve<MiddlewareBuilderWithAutofac>();
                    middlewareBuilder
                        .UseControllers
                        (
                            new PhysicalFileController(),
                            new JsonConfigurationController(ProgramInfo.CurrentDirectory, "appsettings.json"),
                            new HttpController(new HttpClient(new HttpClientHandler { UseProxy = false })
                            {
                                BaseAddress = new Uri(ProgramInfo.Configuration["mailr:BaseUri"])
                            }, ImmutableContainer.Empty.UpdateItem(ResourceControllerProperties.Tags, tags => tags.Add("Mailr")))
                        )
                        .UseTelemetry(ctx.Resolve<ILogger<TelemetryMiddleware>>())
                        .Use<EnvironmentVariableMiddleware>();

                    return new ResourceSquid(middlewareBuilder.Build<ResourceContext>());
                })
                .As<IResourceSquid>();
            
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
}