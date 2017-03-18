using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autofac;
using Gunter.Data.Configurations;
using Gunter.Alerts;
using Newtonsoft.Json;
using SmartConfig.DataStores.AppConfig;
using Reusable.Logging;
using Reusable;
using Gunter.Data;
using System.Reflection;
using Gunter.Services;

namespace Gunter
{
    internal class Program
    {
        public static readonly string InstanceName = Assembly.GetAssembly(typeof(Program)).GetName().Name;
        public static readonly string InstanceVersion = "1.0.0";

        private static readonly ILogger Logger;

        static Program()
        {
            InitializeLogging();
            Logger = LoggerFactory.CreateLogger("Program");
            LogEntry.New().Trace().Message("Logging initialized. Starting...").Log(Logger);
        }

        private static int Main(string[] args)
        {
            try
            {
                InitializeConfiguration();

                var container = InitializeContainer();

                var globals = InitializeGlobals(
                    PathResolver.Resolve(AppSettingsConfig.TestsDirectoryName,
                    $"{InstanceName}.Globals.json")
                );

                var profile = args.FirstOrDefault();
                if (!string.IsNullOrEmpty(profile)) globals = globals.Add(Globals.TestCase.Profile, profile);

                var tests = InitializeTests(
                    Directory.GetFiles(PathResolver.Resolve(AppSettingsConfig.TestsDirectoryName, string.Empty),
                    $"{InstanceName}.Tests.*.json"),
                    container
                ).ToList();

                LogEntry.New().Trace().Message("Validating test configurations.").Log(Logger);
                foreach (var config in tests)
                {
                    TestConfigurationValidator.ValidateDataSources(config, Logger);
                    TestConfigurationValidator.ValidateAlerts(config, Logger);
                }

                using (var logEntry = LogEntry.New().Info().Message($"***{InstanceName} v{InstanceVersion} finished. ({{ElapsedSeconds}} sec) ***").AsAutoLog(Logger))
                using (var scope = container.BeginLifetimeScope())
                {
                    LogEntry.New().Info().Message($"*** {InstanceName} v{InstanceVersion} started. ***").Log(Logger);
                    scope.Resolve<TestRunner>().RunTests(tests, globals);
                }

                return 0;
            }
            // Exception should already be logged elsewhere and rethrown to exit the application.
            catch (Exception ex)
            {
                LogEntry.New().Fatal().Message($"***{InstanceName} v{InstanceVersion} crashed. ({{ElapsedSeconds}} sec) ***").Exception(ex).Log(Logger);
                return 1;
            }
        }

        private static void InitializeLogging()
        {
            Reusable.Logging.NLog.Tools.LayoutRenderers.InvariantPropertiesLayoutRenderer.Register();
            Reusable.Logging.NLog.Tools.DatabaseTargetQueryGenerator.GenerateInsertQueries();

            Reusable.Logging.Logger.ComputedProperties.Add(new Reusable.Logging.ComputedProperties.AppSetting(name: "Environment", key: $"{InstanceName}.Environment"));
            Reusable.Logging.Logger.ComputedProperties.Add(new Reusable.Logging.ComputedProperties.ElapsedSeconds());

            Reusable.Logging.LoggerFactory.Initialize<Reusable.Logging.Adapters.NLogFactory>();
        }

        private static void InitializeConfiguration()
        {
            using (var entry = LogEntry.New().Stopwatch(sw => sw.Start()).AsAutoLog(Logger))
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

                    entry.Message("Configuration initialized. ({ElapsedSeconds} sec)");
                }
                catch (Exception ex)
                {
                    entry.Error().Exception(ex).Message("Error initializing configuration. ({ElapsedSeconds} sec)");
                    throw;
                }
            }
        }

        private static IContainer InitializeContainer()
        {
            var containerBuilder = new ContainerBuilder();

            containerBuilder
                .RegisterType<TestRunner>()
                .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(TestRunner))));

            containerBuilder
                .RegisterType<Data.SqlClient.TableOrViewDataSource>()
                .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(Data.SqlClient.TableOrViewDataSource))));

            containerBuilder
                .RegisterType<EmailAlert>()
                .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(EmailAlert))));

            #region Register sections

            containerBuilder
                .RegisterType<Alerts.Sections.Text>()
                .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(Alerts.Sections.Text))));

            containerBuilder
                .RegisterType<Alerts.Sections.DataSourceInfo>()
                .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(Alerts.Sections.DataSourceInfo))));

            containerBuilder
                .RegisterType<Alerts.Sections.DataAggregate>()
                .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(Alerts.Sections.DataAggregate))));

            #endregion

            return containerBuilder.Build();
        }

        private static IConstantResolver InitializeGlobals(string fileName)
        {
            var globals = new ConstantResolver(Globals.Default) as IConstantResolver;
            Globals.ValidateNames(globals);

            globals = globals.Add(Globals.Environment, AppSettingsConfig.Environment);

            if (File.Exists(fileName))
            {
                globals = globals.UnionWith(JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(fileName)));
            }

            return globals;
        }

        private static IEnumerable<TestConfiguration> InitializeTests(IEnumerable<string> fileNames, IContainer container)
        {
            LogEntry.New().Trace().Message("Initializing tests...").Log(Logger);

            return fileNames.Select(LoadTest).Where(Conditional.IsNotNull);

            TestConfiguration LoadTest(string fileName)
            {
                using (var logEntry = LogEntry.New().Info().AsAutoLog(Logger))
                {
                    try
                    {
                        var json = File.ReadAllText(fileName);
                        var test = JsonConvert.DeserializeObject<TestConfiguration>(json, new JsonSerializerSettings
                        {
                            ContractResolver = new AutofacContractResolver(container),
                            DefaultValueHandling = DefaultValueHandling.Populate,
                            TypeNameHandling = TypeNameHandling.Auto
                        });
                        test.FileName = fileName;
                        logEntry.Message($"Loaded '{fileName}'. ({{ElapsedSeconds}} sec)");
                        return test;
                    }
                    catch (Exception ex)
                    {
                        logEntry.Error().Message($"Error loading '{fileName}'. ({{ElapsedSeconds}} sec)").Exception(ex);
                        return null;
                    }
                }
            }
        }
    }
}
