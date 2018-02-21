using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Autofac;
using Gunter.Data;
using Gunter.Json.Converters;
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
using Reusable.SmartConfig.Data;
using Reusable.SmartConfig.DataStores;
using Reusable.SmartConfig.SettingConverters;
using Reusable.SmartConfig.Utilities;

namespace Gunter
{
    internal abstract class Program
    {
        private static ILogger _logger;

        public static readonly string ElapsedFormat = @"mm\:ss\.fff";

        public static readonly string Product = "Gunter";

        public static readonly string Version = "4.1.0";

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
                var loggerFactory = InitializeLogging();
                _logger = loggerFactory.CreateLogger<Program>();

                var container = default(IContainer);
                var scope = default(ILifetimeScope);
                try
                {
                    var configuration = InitializeConfiguration();

                    configuration.AssignValue(() => Environment);
                    configuration.AssignValue(() => TestsDirectoryName);
                    configuration.AssignValue(() => ThemesDirectoryName);

                    _logger.Log(Abstraction.Layer.Infrastructure().Data().Property(new { Environment, TestsDirectoryName, ThemesDirectoryName }));

                    container = InitializeContainer(loggerFactory, configuration);
                    scope = container.BeginLifetimeScope();

                    var testLoader = scope.Resolve<ITestLoader>();
                    var testRunner = scope.Resolve<ITestRunner>();

                    _logger.Log(Abstraction.Layer.Infrastructure().Action().Finished("Initialization"));
                    _logger.Log(Abstraction.Layer.Infrastructure().Data().Property(new { Version }));

                    var tests = testLoader.LoadTests(TestsDirectoryName).ToList();
                    testRunner.RunTests(tests, args.Select(SoftString.Create));

                    _logger.Log(Abstraction.Layer.Infrastructure().Data().Variable(new { ExitCode = ExitCode.Success }));
                    _logger.Log(Abstraction.Layer.Infrastructure().Action().Finished(nameof(Main)));

                    return (int)ExitCode.Success;
                }
                catch (InitializationException ex)
                {
                    _logger.Log(Abstraction.Layer.Infrastructure().Data().Variable(new { ex.ExitCode }));
                    _logger.Log(Abstraction.Layer.Infrastructure().Action().Failed(nameof(Main)), log => log.Exception(ex));

                    /* just prevent double logs */
                    return (int)ex.ExitCode;
                }
                catch (Exception ex)
                {
                    _logger.Log(Abstraction.Layer.Infrastructure().Data().Variable(new { ExitCode = ExitCode.RuntimeFault }));
                    _logger.Log(Abstraction.Layer.Infrastructure().Action().Failed(nameof(Main)), log => log.Exception(ex));

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

        private static ILoggerFactory InitializeLogging()
        {
            try
            {
                Reusable.Utilities.ThirdParty.NLog.LayoutRenderers.SmartPropertiesLayoutRenderer.Register();

                var loggerFactory = new LoggerFactory
                {
                    Observers =
                    {
                        NLogRx.Create()
                    },
                    Configuration = new LoggerConfiguration
                    {
                        Attachements =
                        {
                            new AppSetting("Environment", "Environment"),
                            //new AppSetting("Product", "Product"),
                            new Lambda("Product", _ => Product), // FullName),
                            new Timestamp<UtcDateTime>(),
                            new Snapshot(new JsonStateSerializer
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
                            }),
                            new Reusable.OmniLog.Attachements.Scope(new JsonStateSerializer
                            {
                                Settings = new JsonSerializerSettings
                                {
                                    NullValueHandling = NullValueHandling.Include,
                                    Formatting = Formatting.Indented,
                                    Converters =
                                    {
                                        new StringEnumConverter(),
                                        new SoftStringConverter(),
                                    }
                                }
                            }),
                            new Reusable.OmniLog.Attachements.ElapsedMilliseconds("Elapsed"),
                        }
                    }
                };

                return loggerFactory;
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
                var settingConverter = new JsonSettingConverter(typeof(string));

                var configuration = new Configuration(new ISettingDataStore[]
                {
                    new AppSettings(settingConverter),
                    new InMemory(settingConverter)
                    {
                        new Setting("LookupPaths")
                        {
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
                    },
                });

                _logger.Log(Abstraction.Layer.Infrastructure().Action().Finished(nameof(InitializeConfiguration)));
                return configuration;
            }
            catch (Exception innerException)
            {
                _logger.Log(Abstraction.Layer.Infrastructure().Action().Failed(nameof(InitializeConfiguration)), log => log.Exception(innerException));
                throw new InitializationException(innerException, ExitCode.ConfigurationInitializationFault);
            }
        }

        private static IContainer InitializeContainer(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            try
            {
                var container = Modules.Main.Create(loggerFactory, configuration);

                _logger.Log(Abstraction.Layer.Infrastructure().Action().Finished(nameof(InitializeContainer)));

                return container;
            }
            catch (Exception innerException)
            {
                _logger.Log(Abstraction.Layer.Infrastructure().Action().Failed(nameof(InitializeContainer)), log => log.Exception(innerException));
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
