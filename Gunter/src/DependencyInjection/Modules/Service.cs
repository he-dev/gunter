using System;
using System.Collections.Immutable;
using System.Net.Http;
using Autofac;
using Gunter.Data;
using Gunter.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Reusable;
using Reusable.Commander;
using Reusable.Commander.DependencyInjection;
using Reusable.Data;
using Reusable.IO;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.Translucent;
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
                        serializer.Converters.Add(new LambdaJsonConverter<Option<LogLevel>>
                        {
                            ReadJsonCallback = Option<LogLevel>.Parse
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

            builder
                .RegisterModule(new CommanderModule
                {
                    Command.Registration<Commands.Run>(),
                    Command.Registration<Commands.Send>(),
                    Command.Registration<Commands.Halt>(),
                });
        }
    }

    internal class GunterResourceSetup
    {
        public void ConfigureResources(IResourceCollection controller)
        {
            controller.AddPhysicalFile(default);
            controller.AddJsonFile(default, ProgramInfo.CurrentDirectory, "appsettings.json");
            controller.AddHttp(default, new HttpClient(new HttpClientHandler { UseProxy = false }) { BaseAddress = new Uri(ProgramInfo.Configuration["mailr:BaseUri"]) }, http => { http.Tags.Add("Mailr"); });
        }

        public void ConfigurePipeline(IPipelineBuilder<ResourceContext> pipeline, IServiceProvider serviceProvider)
        {
            pipeline
                .UseTelemetry(serviceProvider.Resolve<ILogger<TelemetryMiddleware>>())
                .UseEnvironmentVariable()
                .UseMiddleware<ResourceExistsValidationMiddleware>();
        }
    }
}