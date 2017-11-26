using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Autofac;
using Gunter.Data;
using Gunter.JsonConverters;
using Gunter.Modules;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Reusable;
using Reusable.DateTimes;
using Reusable.OmniLog;
using Reusable.OmniLog.Attachements;
using Reusable.OmniLog.SemLog;
using Reusable.SmartConfig;
using Reusable.SmartConfig.Binding;
using Reusable.SmartConfig.Data;

namespace Gunter
{
    internal abstract class Program
    {
        private static ILogger _logger;

        public static readonly string ElapsedFormat = @"mm\:ss\.fff";

        public static readonly string Product = "Gunter";

        public static readonly string Version = "3.0.0";

        public static readonly string FullName = $"{Product}-v{Version}";

        [Required]
        public static string Environment { get; set; }

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

                var container = default(IContainer);
                var scope = default(ILifetimeScope);
                try
                {
                    var configuration = InitializeConfiguration();

                    configuration.Bind(() => Environment);
                    configuration.Bind(() => TestsDirectoryName);
                    configuration.Bind(() => ThemesDirectoryName);

                    _logger.State(Layer.Application, Snapshot.Properties<Program>(new { Environment, TestsDirectoryName, ThemesDirectoryName }));

                    container = InitializeContainer(loggerFactory, configuration);
                    scope = container.BeginLifetimeScope();

                    var testLoader = scope.Resolve<ITestLoader>();
                    var testRunner = scope.Resolve<ITestRunner>();

                    _logger.Event(Layer.Application, "Initialization", Result.Success);

                    var tests = testLoader.LoadTests(TestsDirectoryName).ToList();
                    testRunner.RunTests(tests, args.Select(SoftString.Create));

                    _logger.State(Layer.Application, () => (nameof(ExitCode), ExitCode.Success.ToString()));
                    _logger.Success(Layer.Application);

                    return ExitCode.Success;
                }
                catch (InitializationException ex)
                {
                    _logger.State(Layer.Application, () => (nameof(ExitCode), ex.ExitCode.ToString()));
                    _logger.Event(Layer.Application, Reflection.CallerMemberName(), Result.Failure);

                    /* just prevent double logs */
                    return ex.ExitCode;
                }
                catch (Exception ex)
                {
                    _logger.State(Layer.Application, () => (nameof(ExitCode), ExitCode.RuntimeFault.ToString()));
                    _logger.Failure(Layer.Application, ex);
                    return ExitCode.RuntimeFault;
                }
                finally
                {
                    scope?.Dispose();
                    container?.Dispose();
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
                            //new AppSetting("Product", "Product"),
                            new Lambda("Product", log => FullName),
                            new Timestamp<UtcDateTime>(),
                            new Reusable.OmniLog.SemLog.Attachements.Snapshot
                            {
                                Settings = new JsonSerializerSettings
                                {
                                    Formatting = Formatting.Indented,
                                    Converters =
                                    {
                                        new SoftStringConverter(),
                                        new LogLevelConverter(),
                                        new StringEnumConverter()
                                    }
                                }
                            }
                        }
                    }
                };

                var logger = loggerFactory.CreateLogger(nameof(Program));

                logger.Success(Layer.Application);

                return (loggerFactory, logger);
            }
            catch (Exception innerException)
            {
                //_logger.Failure(Layer.Application, innerException); // logger failed to initialize so it cannot be used
                throw new InitializationException(innerException, ExitCode.LoggingIniializationFault);
            }
        }

        private static IConfiguration InitializeConfiguration()
        {
            try
            {
                var configuration = new Configuration(config => config
                    .UseJsonConverter()
                    .UseAppSettings()
                    .UseInMemory(new[]
                    {
                        new Setting
                        {
                            Name = "LookupPaths",
                            Value = new[]
                            {
                                AppDomainPaths.ConfigurationFile(),
                                AppDomainPaths.BaseDirectory(),
                                AppDomainPaths.ApplicationBase(),
                                AppDomainPaths.PrivateBin()
                            }
                            .SelectMany(path => path)
                            .Distinct()
                            .ToList()
                        }
                    })
                );
                _logger.Success(Layer.Application);
                return configuration;
            }
            catch (Exception innerException)
            {
                _logger.Failure(Layer.Application, innerException);
                throw new InitializationException(innerException, ExitCode.ConfigurationInitializationFault);
            }
        }

        private static IContainer InitializeContainer(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            try
            {
                var container = Modules.Main.Create(loggerFactory, configuration);

                _logger.Success(Layer.Application);

                return container;
            }
            catch (Exception innerException)
            {
                _logger.Failure(Layer.Application, innerException);
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
}
