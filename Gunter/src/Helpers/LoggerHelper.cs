using System;
using Reusable.Exceptionize;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Connectors;
using Reusable.OmniLog.Extensions;
using Reusable.OmniLog.Nodes;
using Reusable.OmniLog.Properties;

namespace Gunter.Helpers {
    public static class LoggerHelper
    {
        public static ILoggerFactory InitializeLoggerFactory()
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