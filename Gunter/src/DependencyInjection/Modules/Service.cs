using System;
using System.Collections.Generic;
using System.Net.Http;
using Autofac;
using Gunter.Data;
using Gunter.Services;
using Gunter.Workflow.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Reusable;
using Reusable.Commander;
using Reusable.Commander.DependencyInjection;
using Reusable.Data;
using Reusable.Extensions;
using Reusable.Flowingo.Steps;
using Reusable.IO;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.Translucent;
using Reusable.Translucent.Abstractions;
using Reusable.Translucent.Controllers;
using Reusable.Translucent.Middleware;
using Reusable.Translucent.Middleware.ResourceValidator;
using Reusable.Utilities.Autofac;
using Reusable.Utilities.JsonNet;
using Reusable.Utilities.JsonNet.Converters;
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

            // builder
            //     .RegisterType<MiddlewareBuilderWithAutofac>();

            // builder
            //     .RegisterType<AutofacServiceProvider>()
            //     .As<IServiceProvider>();

            builder
                .Register(ctx =>
                {
                    return new Resource(new IResourceMiddleware[]
                    {
                        new ResourceTelemetry(ctx.Resolve<ILogger<ResourceTelemetry>>()),
                        new EnvironmentVariableResourceMiddleware(),
                        new ResourceValidation(new CompositeResourceValidator
                        {
                            new RequiredResourceExists()
                        }),
                        new ResourceSearch(new IResourceController[]
                        {
                            new PhysicalFileResourceController(),
                            new JsonFileController(ProgramInfo.CurrentDirectory, "appsettings.json"),
                            new HttpController
                            (
                                new HttpClient(new HttpClientHandler { UseProxy = false })
                                {
                                    BaseAddress = new Uri(ProgramInfo.Configuration["mailr:BaseUri"])
                                }
                            ).Pipe(x => x.Tags.Add("Mailr")),
                        }),
                    });
                })
                .As<IResource>();

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

            // builder
            //     .RegisterInstance(RuntimeProperty.BuiltIn.Enumerate());

            builder
                .RegisterModule<JsonContractResolverModule>();

            // builder
            //     .RegisterType<RuntimePropertyNameValidator>()
            //     .As<IRuntimePropertyNameValidator>();

            builder
                .RegisterType<Workflow<TheoryContext>>()
                .InstancePerDependency();
            
            builder
                .RegisterType<Workflow<TestContext>>()
                .InstancePerDependency();

            // builder
            //     .RegisterType<TestLoader>()
            //     .As<ITestLoader>();

            // builder
            //     .RegisterType<TestComposer>()
            //     .As<ITestComposer>();
            //
            // builder
            //     .RegisterType<TestRunner>()
            //     .As<ITestRunner>();

            // builder
            //     .RegisterType<RuntimePropertyProvider>()
            //     .WithParameter(new TypedParameter(typeof(IEnumerable<IProperty>), RuntimeProperty.BuiltIn.Enumerate()))
            //     .AsSelf();

            builder
                .RegisterModule(new CommandModule(builder =>
                {
                    builder.Register<Commands.Run>();
                    //builder.Register<Commands.Send>();
                    //builder.Register<Commands.Halt>();
                }));
        }
    }
}