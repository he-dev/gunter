using System;
using Autofac;
using Gunter.Helpers;
using Reusable.Commander.DependencyInjection;
using Reusable.Extensions;
using Reusable.IO;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Nodes;
using Reusable.Utilities.Autofac.JsonNet;

namespace Gunter.DependencyInjection
{
    public class ContainerFactory
    {
        public static IContainer CreateContainer() => CreateContainer(LoggerHelper.InitializeLoggerFactory());

        public static IContainer CreateContainer(ILoggerFactory loggerFactory, Action<ContainerBuilder>? configure = default)
        {
            using var logger = loggerFactory.CreateLogger<ContainerFactory>().BeginScope(nameof(CreateContainer));
            try
            {
                var builder = new ContainerBuilder();

                builder.RegisterOmniLog(loggerFactory);

                builder
                    .RegisterType<PhysicalDirectoryTree>()
                    .As<IDirectoryTree>();

                builder
                    .RegisterModule<JsonContractResolverModule>();

                builder.RegisterModule<Modules.DataModule>();
                builder.RegisterModule<Modules.ResourceModule>();
                builder.RegisterModule<Modules.ServiceModule>();
                builder.RegisterModule<Modules.WorkflowModule>();

                builder.RegisterModule(new CommandModule(builder => builder.Register<Commands.Run>()));

                return builder.Pipe(configure).Build();
            }
            catch (Exception inner)
            {
                logger.Exceptions.Push(inner);
                throw;
            }
        }
    }
}