using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autofac;
using Newtonsoft.Json;
using Reusable.Logging;
using Reusable;
using Gunter.Data;
using System.Reflection;
using Gunter.Data.Configuration;
using Gunter.Messaging.Email;
using Gunter.Reporting;
using Gunter.Reporting.Details;
using Gunter.Services;
using Gunter.Services.Validators;
using Reusable.ConfigWhiz;
using Reusable.ConfigWhiz.Datastores.AppConfig;
using Reusable.Extensions;

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
            Configuration = InitializeConfiguration();
            LogEntry.New().Trace().Message("Logging initialized. Starting...").Log(Logger);
        }

        public static readonly string GlobalsFileName = $"{InstanceName}.Tests.Globals.json";

        public static Configuration Configuration { get; }

        private static int Main(string[] args)
        {
            try
            {
                var container = InitializeContainer();

                var globals = InitializeGlobals(PathResolver.Resolve(Configuration.Load<Program, Global>().TestsDirectoryName, GlobalsFileName));

                var profile = args.FirstOrDefault();
                if (!string.IsNullOrEmpty(profile)) globals = globals.Add(VariableName.TestCase.Profile, profile);

                var testFileNames =
                    Directory
                        .GetFiles(
                            PathResolver.Resolve(Configuration.Load<Program, Global>().TestsDirectoryName, string.Empty),
                            $"{InstanceName}.Tests.*.json")
                        .Where(fileName => !fileName.EndsWith(GlobalsFileName, StringComparison.OrdinalIgnoreCase));

                var tests = InitializeTests(testFileNames, container).ToList();

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
                    scope.Resolve<TestRunner>().RunTestFiles(tests, globals);
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

            Reusable.Logging.Logger.ComputedProperties.Add(new Reusable.Logging.ComputedProperties.AppSetting(name: "Environment", key: $"{InstanceName}.Environment"));
            Reusable.Logging.Logger.ComputedProperties.Add(new Reusable.Logging.ComputedProperties.ElapsedSeconds());

            Reusable.Logging.LoggerFactory.Initialize<Reusable.Logging.Adapters.NLogFactory>();
        }

        private static Configuration InitializeConfiguration()
        {
            var entry = LogEntry.New().Stopwatch(sw => sw.Start()).Message("Configuration initialized. ({ElapsedSeconds} sec)");
            try
            {
                return new Configuration(new AppSettings());
            }
            catch (Exception ex)
            {
                entry.Error().Exception(ex).Message("Error initializing configuration. ({ElapsedSeconds} sec)");
                throw;
            }
            finally
            {
                entry.Log(Logger);
            }
        }

        private static IContainer InitializeContainer()
        {
            var containerBuilder = new ContainerBuilder();

            containerBuilder
                .RegisterType<TestRunner>()
                .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(TestRunner))));

            containerBuilder
                .RegisterType<Data.SqlClient.TableOrView>()
                .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(Data.SqlClient.TableOrView))));

            containerBuilder
                .RegisterType<HtmlEmail>()
                .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(HtmlEmail))));

            #region Initialize reporting componenets

            containerBuilder
                .RegisterType<Report>()
                .As<IReport>();

            containerBuilder
                .RegisterType<Section>()
                .As<ISection>();

            containerBuilder
                .RegisterType<TestCaseInfo>();

            containerBuilder
                .RegisterType<DataSourceInfo>();            

            containerBuilder
                .RegisterType<DataSummary>();

            #endregion

            return containerBuilder.Build();
        }

        private static IVariableResolver InitializeGlobals(string fileName)
        {
            var globals = VariableResolver.Empty;
            GlobalsValidator.ValidateNames(globals, Logger);

            globals = globals.Add(VariableName.Environment, Configuration.Load<Program, Global>().Environment);

            if (File.Exists(fileName))
            {
                globals = globals.UnionWith(JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(fileName)));
            }

            return globals;
        }

        private static IEnumerable<TestFile> InitializeTests(IEnumerable<string> fileNames, IContainer container)
        {
            LogEntry.New().Trace().Message("Initializing tests...").Log(Logger);

            return fileNames.Select(LoadTest).Where(Conditional.IsNotNull);

            TestFile LoadTest(string fileName)
            {
                using (var logEntry = LogEntry.New().Info().AsAutoLog(Logger))
                {
                    try
                    {
                        var json = File.ReadAllText(fileName);
                        var testFile = JsonConvert.DeserializeObject<TestFile>(json, new JsonSerializerSettings
                        {
                            ContractResolver = new AutofacContractResolver(container),
                            DefaultValueHandling = DefaultValueHandling.Populate,
                            TypeNameHandling = TypeNameHandling.Auto
                        });
                        testFile.FileName = fileName;
                        logEntry.Message($"Loaded '{fileName}'. ({{ElapsedSeconds}} sec)");
                        GlobalsValidator.ValidateNames(VariableResolver.Empty.UnionWith(testFile.Locals), Logger);
                        return testFile;
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
