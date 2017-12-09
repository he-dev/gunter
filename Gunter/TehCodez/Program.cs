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
using Reusable.OmniLog.SemanticExtensions;
using Reusable.OmniLog.SemanticExtensions.Attachements;
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

                    _logger.Log(Category.Snapshot.Properties(new { Environment, TestsDirectoryName, ThemesDirectoryName }), Layer.Application);

                    container = InitializeContainer(loggerFactory, configuration);
                    scope = container.BeginLifetimeScope();

                    var testLoader = scope.Resolve<ITestLoader>();
                    var testRunner = scope.Resolve<ITestRunner>();

                    _logger.Log(Category.Action.Finished("Initialization"), Layer.Application);

                    var tests = testLoader.LoadTests(TestsDirectoryName).ToList();
                    testRunner.RunTests(tests, args.Select(SoftString.Create));

                    _logger.Log(Category.Snapshot.Results(new { ExitCode = ExitCode.Success }), Layer.Application);
                    _logger.Log(Category.Action.Finished("Main"), Layer.Application);

                    return (int)ExitCode.Success;
                }
                catch (InitializationException ex)
                {
                    _logger.Log(Category.Snapshot.Results(new { ex.ExitCode }), Layer.Application);
                    _logger.Log(Category.Action.Failed(nameof(Main), ex), Layer.Application);

                    /* just prevent double logs */
                    return (int)ex.ExitCode;
                }
                catch (Exception ex)
                {
                    _logger.Log(Category.Snapshot.Results(new { ExitCode = ExitCode.RuntimeFault }), Layer.Application);
                    _logger.Log(Category.Action.Failed(nameof(Main), ex), Layer.Application);
                    return (int)ExitCode.RuntimeFault;
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
            return (int)ExitCode.UnexpectedFault;
        }

        #region Initialization

        private static (ILoggerFactory, ILogger) InitializeLogging()
        {
            try
            {
                //Reusable.ThirdParty.NLogUtilities.LayoutRenderers.InvariantPropertiesLayoutRenderer.Register();
                Reusable.ThirdParty.NLogUtilities.LayoutRenderers.SmartPropertiesLayoutRenderer.Register();

                var loggerFactory = new LoggerFactory
                {
                    Observers =
                    {
                        NLogRx.Create(Enumerable.Empty<ILogScopeMerge>())
                    },
                    Configuration = new LoggerConfiguration
                    {
                        Attachements =
                        {
                            new AppSetting("Environment", "Environment"),
                            //new AppSetting("Product", "Product"),
                            new Lambda("Product", log => FullName),
                            new Timestamp<UtcDateTime>(),
                            new Snapshot(new JsonSnapshotSerializer
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
                            })
                        }
                    }
                };

                var logger = loggerFactory.CreateLogger(nameof(Program));

                logger.Log(Category.Action.Finished(nameof(InitializeLogging)), Layer.Application);

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
                _logger.Log(Category.Action.Finished(nameof(InitializeConfiguration)), Layer.Application);
                return configuration;
            }
            catch (Exception innerException)
            {
                _logger.Log(Category.Action.Failed(nameof(InitializeConfiguration), innerException), Layer.Application);
                throw new InitializationException(innerException, ExitCode.ConfigurationInitializationFault);
            }
        }

        private static IContainer InitializeContainer(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            try
            {
                var container = Modules.Main.Create(loggerFactory, configuration);

                _logger.Log(Category.Action.Finished(nameof(InitializeContainer)), Layer.Application);

                return container;
            }
            catch (Exception innerException)
            {
                _logger.Log(Category.Action.Failed(nameof(InitializeContainer), innerException), Layer.Application);
                throw new InitializationException(innerException, ExitCode.ContainerInitializationFault);
            }
        }

        #endregion
    }

    internal class InitializationException : Exception
    {
        public InitializationException(Exception innerException, ExitCode exitCode)
            : base(null, innerException)
        {
            ExitCode = exitCode;
        }

        public ExitCode ExitCode { get; }
    }

    internal enum ExitCode
    {
        UnexpectedFault = -1,
        Success = 0,
        LoggingIniializationFault = 1,
        ConfigurationInitializationFault = 2,
        ContainerInitializationFault = 3,
        RuntimeFault = 100,
    }
}
