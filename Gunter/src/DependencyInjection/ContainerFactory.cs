using System;
using Autofac;
using JetBrains.Annotations;
using Reusable;
using Reusable.Exceptionize;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Abstractions.Data;
using Reusable.OmniLog.Computables;
using Reusable.OmniLog.Nodes;
using Reusable.OmniLog.Rx;
using Reusable.OmniLog.Rx.ConsoleRenderers;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.OmniLog.SemanticExtensions.Nodes;

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

                var loggerFactory = new LoggerFactory
                {
                    Nodes =
                    {
                        new ConstantNode
                        {
                            { "Environment", System.Configuration.ConfigurationManager.AppSettings["app:Environment"] },
                            { "Product", ProgramInfo.FullName }
                        },
                        new StopwatchNode
                        {
                            // Selects milliseconds to be logged. This is the default.
                            GetValue = elapsed => elapsed.TotalMilliseconds
                        },
                        new ComputableNode
                        {
                            Computables =
                            {
                                // Adds utc timestamp to each log-entry.
                                new Reusable.OmniLog.Computables.Timestamp<DateTimeUtc>()
                            }
                        },
                        new LambdaNode(),
                        new CorrelationNode(),
                        new SemanticNode(),
                        new DumpNode
                            { },
                        new SerializationNode(),
                        new FilterNode(logEntry => true) { Enabled = false },
                        new RenameNode
                        {
                            Changes =
                            {
                                { CorrelationNode.DefaultLogEntryItemNames.Scope, "Scope" },
                                { DumpNode.DefaultLogEntryItemNames.Variable, "Identifier" },
                                { DumpNode.DefaultLogEntryItemNames.Dump, "Snapshot" },
                            }
                        },
                        new FallbackNode
                        {
                            Defaults =
                            {
                                [LogEntry.BasicPropertyNames.Level] = LogLevel.Information
                            }
                        },
                        new TransactionNode(),
                        new EchoNode
                        {
                            Rx =
                            {
                                new NLogRx(), // Use NLog.
                                new ConsoleRx // Use console.
                                {
                                    Renderer = new SimpleConsoleRenderer
                                    {
                                        Template = @"[{Timestamp:HH:mm:ss:fff}] [{Logger:u}] {Message}"
                                    }
                                }
                            },
                        }
                    }
                };

                return loggerFactory;
            }
            catch (Exception inner)
            {
                throw DynamicException.Create("LoggerInitialization", "Could not initialize logger.", inner);
            }
        }
    }
}