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
using JetBrains.Annotations;
using Reusable.ConfigWhiz;
using Reusable.ConfigWhiz.Datastores.AppConfig;
using Reusable.Extensions;

namespace Gunter
{
    internal class Program
    {
        public static readonly string InstanceName = Assembly.GetAssembly(typeof(Program)).GetName().Name;
        public static readonly string InstanceVersion = "2.0.0";

        private static readonly ILogger Logger;

        static Program()
        {
            Logger = InitializeLogging();
            Configuration = InitializeConfiguration();
        }

        public static readonly string GlobalsFileName = $"{InstanceName}.Tests.Globals.json";

        public static Configuration Configuration { get; }

        private static int Main(string[] args)
        {
            try
            {
                var globals = InitializeGlobals(
                    PathResolver.ResolveFilePath(
                        AppDomain.CurrentDomain, 
                        Path.Combine(Configuration.Load<Program, ProgramConfig>().TestsDirectoryName, GlobalsFileName)));

                var profile = args.FirstOrDefault();
                if (!string.IsNullOrEmpty(profile))
                {
                    globals = globals.Add(VariableName.TestCase.Profile, profile);
                }

                var testFileNames =
                    Directory
                        .GetFiles(
                            PathResolver.ResolveDirectoryPath(AppDomain.CurrentDomain, Configuration.Load<Program, ProgramConfig>().TestsDirectoryName),
                            $"{InstanceName}.Tests.*.json")
                        .Where(fileName => !fileName.EndsWith(GlobalsFileName, StringComparison.OrdinalIgnoreCase));

                var container = InitializeContainer();
                var tests = InitializeTests(testFileNames, container).ToList();

                LogEntry.New().Trace().Message("Validating test configurations.").Log(Logger);
                foreach (var config in tests)
                {
                    TestConfigurationValidator.ValidateDataSources(config, Logger);
                    TestConfigurationValidator.ValidateAlerts(config, Logger);
                }

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
                LogEntry.New().Fatal().Message($"*** {InstanceName} v{InstanceVersion} crashed. ***").Exception(ex).Log(Logger);
                return 1;
            }
            finally
            {
                LogEntry.New().Info().Message($"*** {InstanceName} v{InstanceVersion} exited. ***").Log(Logger);
            }
        }

        #region Initialization

        private static ILogger InitializeLogging()
        {
            Reusable.Logging.NLog.Tools.LayoutRenderers.InvariantPropertiesLayoutRenderer.Register();

            Reusable.Logging.Logger.ComputedProperties.Add(new Reusable.Logging.ComputedProperties.AppSetting(name: "Environment", key: $"{InstanceName}.Environment"));
            Reusable.Logging.Logger.ComputedProperties.Add(new Reusable.Logging.ComputedProperties.ElapsedSeconds());

            Reusable.Logging.LoggerFactory.Initialize<Reusable.Logging.Adapters.NLogFactory>();
            var logger = LoggerFactory.CreateLogger(nameof(Program));
            LogEntry.New().Debug().Message("Logging initialized.").Log(logger);
            return logger;
        }

        private static Configuration InitializeConfiguration()
        {
            try
            {
                return new Configuration(new AppSettings());
            }
            catch (Exception ex)
            {
                throw new InitializationException("Could not initialize configuration.", ex);
            }
        }

        private static IContainer InitializeContainer()
        {
            try
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
            catch (Exception ex)
            {
                throw new InitializationException("Could not initialize container.", ex);
            }
        }

        private static IVariableResolver InitializeGlobals(string fileName)
        {
            try
            {
                var globals = VariableResolver.Empty;

                if (File.Exists(fileName))
                {
                    globals = globals.MergeWith(JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(fileName)));
                    VariableValidator.ValidateNoReservedNames(globals);
                }

                globals = globals.Add(VariableName.Environment, Configuration.Load<Program, ProgramConfig>().Environment);

                return globals;
            }
            catch (Exception ex)
            {
                throw new InitializationException("Could not initialize globals.", ex);
            }
        }

        [NotNull]
        [ItemNotNull]
        private static IEnumerable<TestFile> InitializeTests(IEnumerable<string> fileNames, IContainer container)
        {
            LogEntry.New().Debug().Message("Initializing tests.").Log(Logger);

            return fileNames.Select(LoadTest).Where(Conditional.IsNotNull);

            TestFile LoadTest(string fileName)
            {
                var logEntry = LogEntry.New().Info();
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
                    VariableValidator.ValidateNoReservedNames(VariableResolver.Empty.MergeWith(testFile.Locals));
                    logEntry.Message($"Loaded: {fileName}");
                    return testFile;
                }
                catch (Exception ex)
                {
                    logEntry.Error().Message($"Could not load: {fileName}").Exception(ex);
                    return null;
                }
                finally
                {
                    logEntry.Log(Logger);
                }
            }
        }
        
        #endregion
    }

    internal class InitializationException : Exception
    {
        public InitializationException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
