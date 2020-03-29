using System;
using System.Net.Http;
using Autofac;
using Gunter.Data.ReportSections;
using Newtonsoft.Json.Serialization;
using Reusable.OmniLog.Abstractions;
using Reusable.Translucent;
using Reusable.Translucent.Abstractions;
using Reusable.Translucent.Controllers;
using Reusable.Translucent.Middleware;
using Reusable.Translucent.Middleware.ResourceValidator;

namespace Gunter.DependencyInjection.Modules
{
    internal class ResourceModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(ctx =>
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
                            Serializer =
                            {
                                ContractResolver = new DefaultContractResolver
                                {
                                    NamingStrategy = new CamelCaseNamingStrategy()
                                },
                                Converters =
                                {
                                    new PrettyJsonConverter()
                                }
                            },
                            Name = "Mailr"
                        },
                    }),
                });
            }).As<IResource>();
        }
    }
}