using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Gunter.Data;
using Gunter.Json.Converters;
using JetBrains.Annotations;
using MailrNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
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
using Reusable.Utilities.ThirdParty.JsonNet;
using Reusable.Utilities.ThirdParty.JsonNet.Serialization;

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

        public Program(ILogger<Program> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
        }

        internal static async Task<int> Main(string[] args)
        {
            try
            {
                var loggerFactory = InitializeLogging();
                var configuration = InitializeConfiguration();

                await StartAsync(args, loggerFactory, configuration);

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

        public static async Task StartAsync(string[] args, ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            var logger = loggerFactory.CreateLogger<Program>();
            try
            {
                using (var container = DependencyInjection.Program.Create(loggerFactory, configuration))
                using (var scope = container.BeginLifetimeScope())
                {
                    var program = scope.Resolve<Program>();

                    logger.Log(Abstraction.Layer.Infrastructure().Variable(new { program.FullName }));
                    logger.Log(Abstraction.Layer.Infrastructure().Routine(nameof(StartAsync)).Running());

                    var testLoader = scope.Resolve<ITestLoader>();
                    var testComposer = scope.Resolve<TestComposer>();
                    var testRunner = scope.Resolve<ITestRunner>();

                    Directory.SetCurrentDirectory(program.CurrentDirectory);

                    logger.Log(Abstraction.Layer.Infrastructure().Property(new { program.CurrentDirectory }));


                    var tests = testLoader.LoadTests(program.TestsDirectoryName).ToList();
                    var compositions = testComposer.ComposeTests(tests).ToList();

                    var profiles = args.Select(SoftString.Create);
                    var tasks = compositions.Select(testFile => testRunner.RunTestsAsync(testFile, profiles)).ToArray();

                    await Task.WhenAll(tasks);
                }

                logger.Log(Abstraction.Layer.Infrastructure().Routine(nameof(StartAsync)).Completed());
            }
            catch (Exception ex)
            {
                logger.Log(Abstraction.Layer.Infrastructure().Routine(nameof(StartAsync)).Faulted(), ex);
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
                                    },
                                    ContractResolver = new CompositeContractResolver
                                    {
                                        new InterfaceContractResolver<ILogScope>(),
                                        new DefaultContractResolver()
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
                        { nameof(CurrentDirectory), Path.GetDirectoryName(typeof(Program).Assembly.Location) },
                        { nameof(Product), "Gunter" },
                        { nameof(Version), "4.1.0" },
                        { nameof(ElapsedFormat), ElapsedFormat }
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
