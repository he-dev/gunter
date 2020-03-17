using System;
using Autofac;
using JetBrains.Annotations;
using Reusable;
using Reusable.Exceptionize;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Services;

namespace Gunter.DependencyInjection
{
    public static class ContainerFactory
    {
        public static IContainer CreateContainer() => CreateContainer(InitializeLogging());

        public static IContainer CreateContainer(ILoggerFactory loggerFactory, Action<ContainerBuilder> configureContainer = default)
        {
            try
            {
                var builder = new ContainerBuilder();

                builder
                    .RegisterModule(new LoggerModule(loggerFactory));

                // todo - this should be removed
                builder
                    .RegisterType<ProgramInfo>()
                    .AsSelf();


                builder.RegisterModule<Modules.Service>();
                builder.RegisterModule<Modules.Data>();
                builder.RegisterModule<Modules.Reporting>();
                builder.RegisterModule<Modules.Mailr>();
                builder.RegisterModule<Modules.Mailr>();
                builder.RegisterModule<Modules.WorkflowModule>();

                configureContainer?.Invoke(builder);

                return builder.Build();
            }
            catch (Exception inner)
            {
                throw DynamicException.Create("ContainerInitialization", "Could not initialize program container.", inner);
            }
        }

        internal static ILoggerFactory InitializeLogging()
        {
            try
            {
                Reusable.Utilities.NLog.LayoutRenderers.SmartPropertiesLayoutRenderer.Register();

                return
                    LoggerFactory
                        .Builder()
                        .UseService
                        (
                            new Constant("Environment", System.Configuration.ConfigurationManager.AppSettings["app:Environment"]),
                            new Constant("Product", ProgramInfo.FullName),
                            new Timestamp<DateTimeUtc>()
                        )
                        .UseDelegate()
                        .UseScope()
                        .UseBuilder()
                        .UseDestructure()
                        //.UseObjectMapper()
                        .UseSerializer()
                        .UsePropertyMapper
                        (
                            (LogProperty.Names.Scope, "Scope"),
                            (LogProperty.Names.SnapshotName, "Identifier"),
                            (LogProperty.Names.Snapshot, "Snapshot")
                        )
                        .UseFallback
                        (
                            (LogProperty.Names.Level, LogLevel.Information)
                        )
                        .UseCamelCase()
                        .UseEcho
                        (
                            new NLogRx(),
                            new SimpleConsoleRx
                            {
                                Template = @"[{Timestamp:HH:mm:ss:fff}] [{Level}] {Layer} | {Category} | {Identifier}: {Snapshot} {Elapsed}ms | {Message} {Exception}"
                            }
                        )
                        .Build();
            }
            catch (Exception inner)
            {
                throw DynamicException.Create("LoggerInitialization", "Could not initialize logger.", inner);
            }
        }
    }
}