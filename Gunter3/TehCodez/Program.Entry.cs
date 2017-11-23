using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Gunter.Services;
using Reusable.DateTimes;
using Reusable.OmniLog;
using Reusable.OmniLog.Attachements;
using Reusable.OmniLog.SemLog.Attachements;
using Reusable.SmartConfig;

namespace Gunter
{
    internal partial class Program
    {
        internal static int Main(string[] args)
        {
            var loggerFactory = InitializeLogging();
            var configuration = InitializeConfiguration();

            var builder = new ContainerBuilder();
            builder.RegisterModule(new GunterModule(loggerFactory, configuration));

            using (var container = builder.Build())
            using (var scope = container.BeginLifetimeScope())
            {
                var testLoader = scope.Resolve<ITestLoader>();
                var current = testLoader.LoadTests(@"C:\");

                var testRunner = scope.Resolve<ITestRunner>();
                testRunner.RunTests(current.GlobalTestFile, current.TestFiles, args);
            }

            return 0;
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
                        mainLogger.LogEntry.Message($"Created Gunter v{Version}");
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

        internal static ILoggerFactory InitializeLogging()
        {
            try
            {
                Reusable.ThirdParty.NLogUtilities.LayoutRenderers.InvariantPropertiesLayoutRenderer.Register();
                Reusable.ThirdParty.NLogUtilities.LayoutRenderers.IgnoreCaseEventPropertiesLayoutRenderer.Register();

                var loggerFactory = new LoggerFactory(new[]
                {
                    NLogRx.Create(Enumerable.Empty<ILogScopeMerge>())
                })
                {
                    Configuration = new LoggerConfiguration
                    {
                        Attachements = new HashSet<ILogAttachement>(AppSetting.FromAppConfig("omnilog:", "Environment", "Product"))
                        {
                            new Timestamp<UtcDateTime>(),
                            new Snapshot()
                        }
                    }
                };

                //Logger.Configuration = new LoggerConfiguration
                //{
                //    ComputedProperties = { new ElapsedSeconds(), new AppSetting(name: "Environment", key: "Environment") },
                //    Recorders = { new NLogRecorder("NLog") },
                //    Filters =
                //    {
                //        new LogFilter
                //        {
                //            LogLevel = LogLevel.Debug,
                //            Recorders = { "NLog" }
                //        }
                //    }
                //};

                //Logger.Create<Program>().Log(e => e.Debug().Message("Logging initialized."));

                return loggerFactory;
            }
            catch (Exception ex)
            {
                throw new InitializationException("Could not initialize configuration.", ex, ExitCode.LoggingError);
            }
        }

        internal static IConfiguration InitializeConfiguration()
        {
            try
            {
                var configuration = new Configuration(new AppSettings());

                //Logger.Create<Program>().Log(e => e.Debug().Message("Configuration initialized."));

                return configuration;
            }
            catch (Exception ex)
            {
                throw new InitializationException("Could not initialize configuration.", ex, ExitCode.ConfigurationError);
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
