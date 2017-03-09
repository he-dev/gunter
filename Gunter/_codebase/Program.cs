using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Autofac;
using Gunter.Data.Configurations;
using Gunter.Alerting;
using Gunter.Alerting.Email;
using Gunter.Testing;
using Newtonsoft.Json;
using SmartConfig.DataStores.AppConfig;
using Reusable.Logging;
using Reusable;
using Gunter.Data;
using System.Reflection;
using Gunter.Services;

// ReSharper disable UseStringInterpolation

namespace Gunter
{
    internal class Program
    {
#if DEBUG
        public const string DefaultInstanceName = "Gunter.debug";
#else
        public const string DefaultInstanceName = "Gunter";
#endif        

        private static ILogger _logger;

        private static IContainer _container;

        private static int Main(string[] args)
        {
            try
            {
                InitializeLogger();

                LogEntry.New().Message("*** Gunter v3.0.0 ***").Log(_logger);

                InitializeConfiguration();
                InitializeContainer();


                using (var scope = _container.BeginLifetimeScope())
                {
                    var testRunner = scope.Resolve<TestRunner>();
                    var testConfigs = ReadTestConfigs().ToArray();
                    var constants = scope.Resolve<IConstantResolver>();

                    testRunner.RunTests(testConfigs, constants);
                }

                return 0;
            }
            // Exception should already be logged elsewhere and rethrown to exit the application.
            catch (Exception ex)
            {
                LogEntry.New().Fatal().Message("Crashed.").Exception(ex).Log(_logger);
                return 1;
            }
        }

        private static void InitializeLogger()
        {
            Reusable.Logging.NLog.Tools.LayoutRenderers.InvariantPropertiesLayoutRenderer.Register();
            Reusable.Logging.NLog.Tools.DatabaseTargetQueryGenerator.GenerateInsertQueries();

            Logger.ComputedProperties.Add(new Reusable.Logging.ComputedProperties.AppSetting("Environment", "Environment"));
            Logger.ComputedProperties.Add(new Reusable.Logging.ComputedProperties.ElapsedSeconds());

            Reusable.Logging.LoggerFactory.Initialize<Reusable.Logging.Adapters.NLogFactory>();
            _logger = LoggerFactory.CreateLogger("Program");

        }

        private static void InitializeContainer()
        {
            var globals = ReadGlobals();
            globals["Environment"] = AppSettingsConfig.Environment;

            var containerBuilder = new ContainerBuilder();

            // Configure dependencies.

            //containerBuilder
            //    .RegisterType<EmailAlert>()
            //    .As<IAlert>();

            //containerBuilder.RegisterInstance(new StateRepository(AppSettingsConfig.Environment));

            containerBuilder
                .RegisterInstance(new ConstantResolver(globals))
                .As<IConstantResolver>();

            containerBuilder
                .RegisterType<Data.SqlClient.TableOrViewDataSource>()
                .As<IDataSource>()
                .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(Data.SqlClient.TableOrViewDataSource))));

            //containerBuilder
            //    .RegisterType<EmailService>()
            //    .As<IEmailService>()
            //    .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.GetLogger<EmailService>()));

            containerBuilder
                .RegisterType<EmailAlert>()
                .As<IAlert>()
                .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(EmailAlert))));

            containerBuilder
                .RegisterType<TestRunner>()
                .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(TestRunner))));

            _container = containerBuilder.Build();
        }

        private static void InitializeConfiguration()
        {
            using (var entry = LogEntry.New().Stopwatch(sw => sw.Start()).AsAutoLog(_logger))
            {
                try
                {
                    //SmartConfig.Configuration.Load
                    //       .From(new ConnectionStringsStore())
                    //       .Select(typeof(ConnectionStringsConfig));

                    SmartConfig.Configuration.Builder()
                           .From(new AppSettingsStore())
                           .Select(typeof(AppSettingsConfig));

                    //SmartConfig.Configuration.Load
                    //    .From(new SQLiteStore("name=ConfigDb"))
                    //    .Where("Environment", AppSettingsConfig.Environment)
                    //    .Register<JsonToObjectConverter<LogInfo>>()
                    //    .Select(typeof(MainConfig));

                    entry.Message("Loaded configuration in {ElapsedSeconds} seconds.");

                }
                catch (Exception ex)
                {
                    entry.Error().Exception(ex).Message("Could not load configuration in {ElapsedSeconds} seconds.");
                    throw;
                }
            }
        }

        private static Dictionary<string, object> ReadGlobals()
        {
            var testDirectoryName = AppSettingsConfig.TestsDirectoryName;

            if (!Path.IsPathRooted(testDirectoryName))
            {
                testDirectoryName = Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(typeof(Program)).Location), testDirectoryName);
            }
            var fileName = Path.Combine(testDirectoryName, "Globals.json");
            if (!File.Exists(fileName))
            {
                return new Dictionary<string, object>();
            }
            var json = File.ReadAllText(fileName);
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        }

        private static IEnumerable<TestConfiguration> ReadTestConfigs()
        {
            var testFileNames = Directory.GetFiles(AppSettingsConfig.TestsDirectoryName, "tests.*.json");

            var loadAudit = new Func<string, TestConfiguration>(fileName =>
            {
                using (var logger = LogEntry.New().AsAutoLog(_logger))
                {
                    try
                    {
                        var json = File.ReadAllText(fileName);
                        var test = JsonConvert.DeserializeObject<TestConfiguration>(json, new JsonSerializerSettings
                        {
                            ContractResolver = new AutofacContractResolver(_container),
                            DefaultValueHandling = DefaultValueHandling.Populate,
                            TypeNameHandling = TypeNameHandling.Auto
                        });
                        logger.Message("Imported \"{fileName}\".".Format(new { fileName }));
                        return test;
                    }
                    catch (Exception ex)
                    {
                        logger.Error().Message("Could not import \"{fileName}\".".Format(new { fileName })).Exception(ex);
                        return null;
                    }
                }
            });

            return testFileNames.Select(loadAudit).Where(test => test != null);
        }

        internal static string CreateInstanceName(string[] args)
        {

            if (!args.Any())
            {
                return DefaultInstanceName;
            }

            // https://regex101.com/r/jj7uq4/1
            // https://regex101.com/delete/cRFp0YnhCmRWG4ZYdvAJrnCz

            var profileMatch = Regex.Match(args.First(), @"-profile:(?<profile>[a-z_][a-z0-9\-\.-]*)", RegexOptions.IgnoreCase);
            return
                profileMatch.Success
                ? string.Format("{0}.{1}", DefaultInstanceName, profileMatch.Groups["profile"].Value)
                : DefaultInstanceName;
        }

        //private static int Exit(ExitCode errorCode)
        //{
        //    _logger.Info().MessageFormat("*** Exited with error code {ErrorCode}.", new { ErrorCode = (int)errorCode }).Log();
        //    _logger.Info().Message("*** Good bye!.").Log();
        //    return (int)errorCode;
        //}
    }

    internal class FileName
    {
        public const string Globals = "Globals";
        public const string Templates = "_Templates";
    }


}
