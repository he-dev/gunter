using System;
using System.Net.Http;
using Autofac;
using Reusable.Commander.DependencyInjection;
using Reusable.IO;
using Reusable.OmniLog.Abstractions;
using Reusable.Translucent;
using Reusable.Translucent.Abstractions;
using Reusable.Translucent.Controllers;
using Reusable.Translucent.Middleware;
using Reusable.Translucent.Middleware.ResourceValidator;
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
                            {
                                CreateHttpClient = () => new HttpClient(new HttpClientHandler { UseProxy = false })
                                {
                                    BaseAddress = new Uri(ProgramInfo.Configuration["mailr:BaseUri"])
                                },
                                Name = "Mailr"
                            },
                        }),
                    });
                })
                .As<IResource>();

            builder
                .RegisterModule<JsonContractResolverModule>();

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