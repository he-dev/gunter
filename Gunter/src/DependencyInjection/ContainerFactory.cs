using System;
using Autofac;
using JetBrains.Annotations;
using Reusable;
using Reusable.Exceptionize;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Abstractions.Data;
using Reusable.OmniLog.Nodes;
using Reusable.OmniLog.Rx;
using Reusable.OmniLog.Rx.ConsoleRenderers;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter.DependencyInjection
{
    public static class ContainerFactory
    {
        public static IContainer CreateContainer() => CreateContainer(InitializeLogging(), _ => { });

        public static IContainer CreateContainer(ILoggerFactory loggerFactory, Action<ContainerBuilder> configureContainer)
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

                configureContainer?.Invoke(builder);

                return builder.Build();
            }
            catch (Exception inner)
            {
                throw DynamicException.Create("ContainerInitialization", "Could not initialize program container.", inner);
            }
        }

        [NotNull]
        internal static ILoggerFactory InitializeLogging()
        {
            try
            {
                Reusable.Utilities.NLog.LayoutRenderers.SmartPropertiesLayoutRenderer.Register();

                return
                    new LoggerFactory()
                        .UseConstant(
                            ("Environment", System.Configuration.ConfigurationManager.AppSettings["app:Environment"]),
                            ("Product", ProgramInfo.FullName))
                        .UseScalar(new Reusable.OmniLog.Scalars.Timestamp<DateTimeUtc>())
                        .UseStopwatch()
                        .UseLambda()
                        .UseCorrelation()
                        .UseBuilder()
                        .UseOneToMany()
                        .UseMapper()
                        .UseSerializer()
                        .UseRename(
                            (LogEntry.Names.Scope, "Scope"),
                            (LogEntry.Names.Object, "Identifier"),
                            (LogEntry.Names.Snapshot, "Snapshot"))
                        .UseFallback(
                            (LogEntry.Names.Level, LogLevel.Information))
                        .UseBuffer()
                        .UseEcho(
                            new NLogRx(), 
                            new ConsoleRx
                            {
                                Renderer = new SimpleConsoleRenderer
                                {
                                    Template = @"[{Timestamp:HH:mm:ss:fff}] [{Level:u}] {Layer} | {Category} | {Identifier}: {Snapshot} {Elapsed}ms | {Message} {Exception}"
                                }
                            });
            }
            catch (Exception inner)
            {
                throw DynamicException.Create("LoggerInitialization", "Could not initialize logger.", inner);
            }
        }
    }
}