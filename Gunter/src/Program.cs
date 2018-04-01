using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Autofac;
using Gunter.Data;
using Gunter.Json.Converters;
using JetBrains.Annotations;
using MailrNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Reusable;
using Reusable.DateTimes;
using Reusable.Exceptionize;
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
    public class Program
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public static readonly string ElapsedFormat = @"mm\:ss\.fff";


        [Required]
        public string Environment => _configuration.GetValue(() => Environment);

        [Required]
        public string TestsDirectoryName => _configuration.GetValue(() => TestsDirectoryName);

        [Required]
        public string MailrBaseUri => _configuration.GetValue(() => MailrBaseUri);

        public string Product => _configuration.GetValue(() => Product);

        public string Version => _configuration.GetValue(() => Version);

        public string FullName => $"{Product}-v{Version}";

        public string CurrentDirectory => _configuration.GetValue(() => CurrentDirectory);

        public Program(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger<Program>();
        }
     
        internal static int Main(string[] args)
        {
            try
            {
                var loggerFactory = InitializeLogging();
                var configuration = InitializeConfiguration();

                Start(args, loggerFactory, configuration);

                return (int)ExitCode.Success;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                if (ex is InitializationException ie)
                {
                    return (int)ie.ExitCode;
                }
                return (int)ExitCode.UnexpectedFault;
            }
        }

        public static void Start(string[] args, ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            var logger = loggerFactory.CreateLogger("Init");
            try
            {
                using (var container = DependencyInjection.Main.Create(loggerFactory, configuration))
                using (var scope = container.BeginLifetimeScope())
                {
                    var program = scope.Resolve<Program>();
                    var testLoader = scope.Resolve<ITestLoader>();
                    var testComposer = scope.Resolve<TestComposer>();
                    var testRunner = scope.Resolve<ITestRunner>();                  

                    Directory.SetCurrentDirectory(program.CurrentDirectory);

                    //_logger.Log(Abstraction.Layer.Infrastructure().Action().Finished("Initialization"));
                    //_logger.Log(Abstraction.Layer.Infrastructure().Data().Property(new { Version }));

                    var tests = testLoader.LoadTests(program.TestsDirectoryName).ToList();
                    var compositions = testComposer.ComposeTests(tests).ToList();
                    testRunner.RunTests(compositions, args.Select(SoftString.Create));

                    //_logger.Log(Abstraction.Layer.Infrastructure().Data().Variable(new { ExitCode = ExitCode.Success }));
                    //_logger.Log(Abstraction.Layer.Infrastructure().Action().Finished(nameof(Main)));
                }

                logger.Log(Abstraction.Layer.Infrastructure().Action().Finished(nameof(Start)));
            }
            catch (Exception ex)
            {
                logger.Log(Abstraction.Layer.Infrastructure().Action().Failed(nameof(Start)), ex);
                throw;
            }
        }

        #region Initialization

        [NotNull]
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
                            new Lambda("Product", _ => "Gunter"), // FullName),
                            new Timestamp<UtcDateTime>(),
                            new Snapshot(new Reusable.OmniLog.SemanticExtensions.JsonSerializer
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
                            new Reusable.OmniLog.Attachements.Scope(new Reusable.OmniLog.SemanticExtensions.JsonSerializer
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
                throw new InitializationException(innerException, ExitCode.LoggingInializationFault);
            }
        }

        [NotNull]
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
                        new Setting(nameof(CurrentDirectory))
                        {
                            Value = Path.GetDirectoryName(typeof(Program).Assembly.Location)
                        },
                        new Setting(nameof(Product))
                        {
                            Value = "Gunter"
                        },
                        new Setting(nameof(Version))
                        {
                            Value = "4.1.0"
                        },
                    },
                });

                return configuration;
            }
            catch (Exception innerException)
            {
                throw new InitializationException(innerException, ExitCode.ConfigurationInitializationFault);
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
        LoggingInializationFault = 1,
        ConfigurationInitializationFault = 2,
        DependencyInjectionInitializationFault = 3,
        RuntimeFault = 100,
    }
}
