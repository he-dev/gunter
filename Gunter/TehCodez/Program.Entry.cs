using System;
using Autofac;
using Gunter.Data;
using Gunter.AutofacModules;
using Gunter.Services;
using Reusable.Logging.Loggex;
using Reusable.Logging.Loggex.ComputedProperties;
using Reusable.Logging.Loggex.Recorders.NLogRecorder.Recorders;
using Reusable.SmartConfig;
using Reusable.SmartConfig.Datastores.AppConfig;
using AutofacModule = Autofac.Module;

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
                configuration => InitializeContainer(configuration, null));
        }

        public static int Start(
            string[] args,
            Action initializeLogging,
            Func<IConfiguration> initializeConfiguration,
            Func<IConfiguration, IContainer> initializeContainer)
        {
            try
            {
                initializeLogging();

                var configuration = initializeConfiguration();
                var container = initializeContainer(configuration);

                var mainLogger = Logger.Create<Program>().BeginLog(e => e.Stopwatch(sw => sw.Start()));
                try
                {
                    using (var scope = container.BeginLifetimeScope())
                    {
                        var program = scope.Resolve<Program>();
                        mainLogger.LogEntry.Message($"Created {Name} v{Version}");
                        program.Start(args);
                    }
                    mainLogger.LogEntry.Message("Completed.");
                }
                catch (Exception ex)
                {
                    mainLogger.LogEntry.Fatal().Message("Crashed.").Exception(ex);
                    return ExitCode.RuntimeError;
                }
                finally
                {
                    mainLogger.EndLog();
                }

                return ExitCode.Success;
            }
            catch (InitializationException ex)
            {
                return ex.ExitCode;
            }
            catch (Exception ex)
            {
                Console.Write("Unexpected exception occured.");
                Console.Write(ex.ToString());
                return ExitCode.UnexpectedError;
            }
        }

        #region Initialization

        internal static void InitializeLogging()
        {
            try
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

                Logger.Create<Program>().Log(e => e.Debug().Message("Logging initialized."));
            }
            catch (Exception ex)
            {
                throw new InitializationException("Could not initialize configuration.", ex, ExitCode.LoggingError);
            }
        }

        internal static Configuration InitializeConfiguration()
        {
            try
            {
                var configuration = new Configuration(new[] { new AppSettings() });

                Logger.Create<Program>().Log(e => e.Debug().Message("Configuration initialized."));

                return configuration;
            }
            catch (Exception ex)
            {
                throw new InitializationException("Could not initialize configuration.", ex, ExitCode.ConfigurationError);
            }
        }

        internal static IContainer InitializeContainer(IConfiguration configuration, AutofacModule overrideModule)
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

                Logger.Create<Program>().Log(e => e.Debug().Message("IoC initialized."));

                return builder.Build();
            }
            catch (Exception ex)
            {
                throw new InitializationException("Could not initialize IoC.", ex, ExitCode.ComponentContainerError);
            }
        }

        #endregion
    }

    internal class InitializationException : Exception
    {
        public InitializationException(string message, Exception innerException, int exitCode)
            : base(message, innerException)
        {
            ExitCode = exitCode;
        }

        public int ExitCode { get; }
    }

    internal static class ExitCode
    {
        public const int Success = 0;
        public const int LoggingError = 1;
        public const int ConfigurationError = 2;
        public const int ComponentContainerError = 3;
        public const int UnexpectedError = 4;
        public const int RuntimeError = 5;
    }
}
