using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Autofac;
using Gunter.Modules;
using Gunter.Services;
using Reusable.DateTimes;
using Reusable.OmniLog;
using Reusable.OmniLog.Attachements;
using Reusable.OmniLog.SemLog;
using Reusable.OmniLog.SemLog.Attachements;
using Reusable.SmartConfig;
using Reusable.SmartConfig.Binding;

namespace Gunter
{
    internal static class Program
    {
        private static ILogger _logger;

        public static readonly string Version = "3.0.0";

        [Required]
        public static string  Environment {get; set; }

        [Required]
        public static string TestsDirectoryName { get; set; }

        [Required]
        public static string ThemesDirectoryName { get; set; }

        internal static int Main(string[] args)
        {
            try
            {
                var (loggerFactory, logger) = InitializeLogging();
                _logger = logger;

                try
                {
                    var configuration = InitializeConfiguration();

                    configuration.Bind(() => Environment);
                    configuration.Bind(() => TestsDirectoryName);
                    configuration.Bind(() => ThemesDirectoryName);

                    using (var container = InitializeContainer(loggerFactory, configuration))
                    using (var scope = container.BeginLifetimeScope())
                    {
                        var testLoader = scope.Resolve<ITestLoader>();
                        var current = testLoader.LoadTests(@"C:\");

                        var testRunner = scope.Resolve<ITestRunner>();
                        testRunner.RunTests(current.GlobalTestFile, current.TestFiles, args);
                    }

                    return ExitCode.Success;
                }
                catch (InitializationException ex)
                {
                    /* just prevent double logs */
                    return ex.ExitCode;
                }
                catch (Exception ex)
                {
                    _logger.Event(Layer.Application, Reflection.CallerMemberName(), Result.Failure, exception: ex);
                    return ExitCode.RuntimeFault;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not initialize logging: {System.Environment.NewLine} {ex}");
            }
            return ExitCode.UnexpectedFault;
        }        

        #region Initialization

        private static (ILoggerFactory, ILogger) InitializeLogging()
        {
            try
            {
                //Reusable.ThirdParty.NLogUtilities.LayoutRenderers.InvariantPropertiesLayoutRenderer.Register();
                Reusable.ThirdParty.NLogUtilities.LayoutRenderers.IgnoreCaseEventPropertiesLayoutRenderer.Register();

                var loggerFactory = new LoggerFactory(new[]
                {
                    NLogRx.Create(Enumerable.Empty<ILogScopeMerge>())
                })
                {
                    Configuration = new LoggerConfiguration
                    {
                        Attachements = new HashSet<ILogAttachement>
                        {
                            new AppSetting("Environment", "Environment"),
                            new AppSetting("Product", "Product"),
                            new Timestamp<UtcDateTime>(),
                            new Snapshot()
                        }
                    }
                };

                var logger = loggerFactory.CreateLogger(nameof(Program));

                logger.Event(Layer.Application, Reflection.CallerMemberName(), Result.Success);

                return (loggerFactory, logger);
            }
            catch (Exception innerException)
            {
                throw new InitializationException(innerException, ExitCode.LoggingIniializationFault);
            }
        }

        private static IConfiguration InitializeConfiguration()
        {
            try
            {
                var configuration = new Configuration(config => config.UseJsonConverter().UseAppSettings());
                _logger.Event(Layer.Application, Reflection.CallerMemberName(), Result.Success);
                return configuration;
            }
            catch (Exception innerException)
            {
                _logger.Event(Layer.Application, Reflection.CallerMemberName(), Result.Failure, exception: innerException);
                throw new InitializationException(innerException, ExitCode.ConfigurationInitializationFault);
            }
        }

        private static IContainer InitializeContainer(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            try
            {
                var builder = new ContainerBuilder();
                builder.RegisterModule(new MainModule(loggerFactory, configuration));

                _logger.Event(Layer.Application, Reflection.CallerMemberName(), Result.Success);

                return builder.Build();
            }
            catch (Exception innerException)
            {
                _logger.Event(Layer.Application, Reflection.CallerMemberName(), Result.Failure, exception: innerException);
                throw new InitializationException(innerException, ExitCode.ContainerInitializationFault);
            }
        }

        #endregion
    }

    internal class InitializationException : Exception
    {
        public InitializationException(Exception innerException, int exitCode)
            : base(null, innerException)
        {
            ExitCode = exitCode;
        }

        public int ExitCode { get; }
    }

    internal static class ExitCode
    {
        public const int UnexpectedFault = -1;
        public const int Success = 0;
        public const int LoggingIniializationFault = 1;
        public const int ConfigurationInitializationFault = 2;
        public const int ContainerInitializationFault = 3;
        public const int RuntimeFault = 100;
    }

    internal static class Event
    {
        public const string InitializeConfiguration = nameof(InitializeConfiguration);
    }
}
