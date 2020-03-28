using System;
using Autofac;
using Reusable.Exceptionize;
using Reusable.Extensions;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Connectors;
using Reusable.OmniLog.Extensions;
using Reusable.OmniLog.Nodes;
using Reusable.OmniLog.Properties;

namespace Gunter.DependencyInjection
{
    public static class ContainerFactory
    {
        public static IContainer CreateContainer() => CreateContainer(InitializeLogging());

        public static IContainer CreateContainer(ILoggerFactory loggerFactory, Action<ContainerBuilder>? configure = default)
        {
            try
            {
                var builder = new ContainerBuilder();

                builder.RegisterOmniLog(loggerFactory);

                builder.RegisterModule<Modules.Service>();
                builder.RegisterModule<Modules.Data>();
                builder.RegisterModule<Modules.Reporting>();
                builder.RegisterModule<Modules.Mailr>();
                builder.RegisterModule<Modules.Mailr>();
                builder.RegisterModule<Modules.WorkflowModule>();

                return builder.Pipe(configure).Build();
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
                    LoggerPipelines
                        .Complete
                        .Configure<AttachProperty>(node =>
                        {
                            node.Properties.Add(new Constant("Environment", System.Configuration.ConfigurationManager.AppSettings["app:Environment"]));
                            node.Properties.Add(new Constant("Product", ProgramInfo.FullName));
                        })
                        .Configure<RenameProperty>(node =>
                        {
                            node.Mappings.Add(Names.Properties.Correlation, "Scope");
                            node.Mappings.Add(Names.Properties.Unit, "Identifier");
                            node.Mappings.Add(Names.Properties.Snapshot, "Snapshot");
                        })
                        .Configure<Echo>(node =>
                        {
                            node.Connectors.Add(new NLogConnector());
                            node.Connectors.Add(new SimpleConsoleRx
                            {
                                Template = @"[{Timestamp:HH:mm:ss:fff}] [{Level}] {Layer} | {Category} | {Identifier}: {Snapshot} {Elapsed}ms | {Message} {Exception}"
                            });
                        })
                        .ToLoggerFactory();
            }
            catch (Exception inner)
            {
                throw DynamicException.Create("LoggerInitialization", "Could not initialize logger.", inner);
            }
        }
    }
}