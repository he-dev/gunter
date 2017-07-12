using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Reusable.Logging;
using Reusable;
using Gunter.Data;
using System.Threading.Tasks;
using Gunter.AutofacModules;
using Gunter.Messaging.Email;
using Gunter.Messaging.Email.ModuleRenderers;
using Gunter.Reporting;
using Gunter.Reporting.Modules;
using Gunter.Services;
using JetBrains.Annotations;
using NLog.Fluent;
using Reusable.Logging.Loggex;
using Reusable.Logging.Loggex.ComputedProperties;
using Reusable.Logging.Loggex.Recorders.NLogRecorder.Recorders;
using Reusable.Markup.Html;
using Reusable.SmartConfig;
using Reusable.SmartConfig.Datastores.AppConfig;
using Module = Gunter.Reporting.Module;

namespace Gunter
{
    internal partial class Program
    {
        internal static int Main(string[] args)
        {
            return Start(
                args,
                InitializeLogging,
                InitializeConfiguration,
                InitializeContainer);
        }

        public static int Start(
            string[] args,
            Action initializeLogging,
            Func<Configuration> initializeConfiguration,
            Func<Configuration, IContainer> initializeContainer)
        {
            initializeLogging();

            var mainLogger = Logger.Create<Program>();
            mainLogger.Log(e => e.Debug().Message("Logging initialized."));
            var mainLogEntry = mainLogger.BeginLog(e => e.Stopwatch(sw => sw.Start()));

            try
            {
                var configuration = initializeConfiguration(); mainLogger.Log(e => e.Debug().Message("Configuration initialized."));
                var container = initializeContainer(configuration); mainLogger.Log(e => e.Debug().Message("IoC initialized."));

                using (var scope = container.BeginLifetimeScope())
                {
                    var program = scope.Resolve<Program>();
                    mainLogger.Log(e => e.Message($"Created {Name} v{Version}"));
                    program.Start(args);
                }

                mainLogEntry.LogEntry.Message("Completed.");
                return 0;
            }
            catch (Exception ex)
            {
                mainLogEntry.LogEntry.Fatal().Message("Crashed.").Exception(ex);
                return 1;
            }
            finally
            {
                mainLogEntry.EndLog();
                mainLogger.Log(e => e.Message("Exited."));
            }
        }

        #region Initialization

        internal static void InitializeLogging()
        {
            Reusable.Logging.NLog.Tools.LayoutRenderers.InvariantPropertiesLayoutRenderer.Register();

            Logger.Configuration = new LoggerConfiguration
            {
                ComputedProperties = { new ElapsedSeconds(), new AppSetting(name: "Environment", key: "Environment") },
                Recorders = { new NLogRecorder("NLog") },
                Filters =
                {
                    new LogFilter
                    {
                        LogLevel = LogLevel.Debug,
                        Recorders = { "NLog" }
                    }
                }
            };            
        }

        internal static Configuration InitializeConfiguration()
        {
            try
            {
                return new Configuration(new[] { new AppSettings() });
            }
            catch (Exception ex)
            {
                throw new InitializationException("Could not initialize configuration.", ex);
            }
        }

        internal static IContainer InitializeContainer(Configuration configuration)
        {
            return InitializeContainer(configuration, null);
        }

        internal static IContainer InitializeContainer(Configuration configuration, Autofac.Module overrideModule)
        {
            try
            {
                var builder = new ContainerBuilder();

                builder.RegisterInstance(configuration.Get<Workspace>());

                builder.RegisterModule<SystemModule>();
                builder.RegisterModule<DataModule>();
                builder.RegisterModule<ReportingModule>();
                builder.RegisterModule<HtmlModule>();

                builder
                    .RegisterType<TestRunner>()
                    .WithParameter(new TypedParameter(typeof(ILogger), Logger.Create(nameof(TestRunner))));

                builder
                    .RegisterType<Program>()
                    .WithParameter(new TypedParameter(typeof(ILogger), Logger.Create(nameof(Program))))
                    .PropertiesAutowired();

                if (overrideModule != null) { builder.RegisterModule(overrideModule); }

                return builder.Build();
            }
            catch (Exception ex)
            {
                throw new InitializationException("Could not initialize container.", ex);
            }
        }

        #endregion
    }

    internal class InitializationException : Exception
    {
        public InitializationException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
