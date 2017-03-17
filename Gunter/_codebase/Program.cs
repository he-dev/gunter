using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Autofac;
using Gunter.Data.Configurations;
using Gunter.Alerts;
using Gunter.Testing;
using Newtonsoft.Json;
using SmartConfig.DataStores.AppConfig;
using Reusable.Logging;
using Reusable;
using Gunter.Data;
using System.Reflection;
using Gunter.Services;
using Gunter.Data.Sections;
using Gunter.Alerts.Sections;

namespace Gunter
{
    internal class Program
    {
        public static readonly string InstanceName = Assembly.GetAssembly(typeof(Program)).GetName().Name;

        private static ILogger _logger;

        private static int Main(string[] args)
        {
            try
            {
                InitializeLogger();
                InitializeConfiguration();

                var container = InitializeContainer();

                var globals = InitializeGlobals(PathResolver.Resolve(AppSettingsConfig.TestsDirectoryName, $"{InstanceName}.Globals.json"));
                var tests = InitializeTests(Directory.GetFiles(PathResolver.Resolve(AppSettingsConfig.TestsDirectoryName, string.Empty), $"{InstanceName}.Tests.*.json"), container);

                using (var logEntry = LogEntry.New().Info().Message("*** Finished in {ElapsedSeconds} sec. ***").AsAutoLog(_logger))
                using (var scope = container.BeginLifetimeScope())
                {
                    LogEntry.New().Info().Message($"*** {InstanceName} v1.0.0 ***").Log(_logger);
                    scope.Resolve<TestRunner>().RunTests(tests, args.FirstOrDefault(), globals);
                }

                return 0;
            }
            // Exception should already be logged elsewhere and rethrown to exit the application.
            catch (Exception ex)
            {
                LogEntry.New().Fatal().Message("*** Crashed ***").Exception(ex).Log(_logger);
                return 1;
            }
        }

        private static void InitializeLogger()
        {
            Reusable.Logging.NLog.Tools.LayoutRenderers.InvariantPropertiesLayoutRenderer.Register();
            Reusable.Logging.NLog.Tools.DatabaseTargetQueryGenerator.GenerateInsertQueries();

            Logger.ComputedProperties.Add(new Reusable.Logging.ComputedProperties.AppSetting(name: "Environment", key: $"{InstanceName}.Environment"));
            Logger.ComputedProperties.Add(new Reusable.Logging.ComputedProperties.ElapsedSeconds());

            Reusable.Logging.LoggerFactory.Initialize<Reusable.Logging.Adapters.NLogFactory>();
            _logger = LoggerFactory.CreateLogger("Program");
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

        private static IContainer InitializeContainer()
        {
            var containerBuilder = new ContainerBuilder();

            containerBuilder
                .RegisterType<Data.SqlClient.TableOrViewDataSource>()
                //.As<IDataSource>()
                .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(Data.SqlClient.TableOrViewDataSource))));

            containerBuilder
                .RegisterType<EmailAlert>()
                //.As<IAlert>()
                .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(EmailAlert))));

            containerBuilder
                .RegisterType<TestRunner>()
                .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(TestRunner))));

            #region Register sections

            containerBuilder
                .RegisterType<Text>()
                //.As<ISectionFactory>()
                .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(Text))));

            containerBuilder
                .RegisterType<DataSourceInfo>()
                //.As<ISectionFactory>()
                .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(DataSourceInfo))));

            containerBuilder
                .RegisterType<DataAggregate>()
                //.As<ISectionFactory>()
                .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(DataAggregate))));

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

        private static List<TestConfiguration> InitializeTests(IEnumerable<string> fileNames, IContainer container)
        {
            return fileNames.Select(LoadTest).Where(Conditional.IsNotNull).ToList();

            TestConfiguration LoadTest(string fileName)
            {
                using (var logEntry = LogEntry.New().Info().AsAutoLog(_logger))
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
                        logEntry.Message($"Read '{fileName}'.");
                        return test;
                    }
                    catch (Exception ex)
                    {
                        logEntry.Error().Message($"Error reading '{fileName}'.").Exception(ex);
                        return null;
                    }
                }
            }
        }
    }
}
