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
using System.Threading.Tasks;
using Gunter.Data.Configuration;
using Gunter.Messaging.Email;
using Gunter.Reporting;
using Gunter.Reporting.Details;
using Gunter.Services;
using Gunter.Services.Validators;
using JetBrains.Annotations;
using NLog.Fluent;
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

        public static readonly IPathResolver PathResolver = new PathResolver();

        private static int Main(string[] args)
        {
            try
            {
                var container = InitializeContainer().GetAwaiter().GetResult();
                using (var scope = container.BeginLifetimeScope())
                {
                    var globals = InitializeGlobals(PathResolver.ResolveFilePath(Path.Combine(Configuration.Load<Program, ProgramConfig>().TestsDirectoryName, GlobalsFileName)));
                    var testFileNames = GetTestFileNames();
                    var tests = InitializeTests(testFileNames, container).ToList();

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

        private static IEnumerable<string> GetTestFileNames()
        {
            var testsDirectoryName = PathResolver.ResolveDirectoryPath(Configuration.Load<Program, ProgramConfig>().TestsDirectoryName);
            return
                from fileName in Directory.GetFiles(testsDirectoryName, $"{InstanceName}.Tests.*.json")
                where !fileName.EndsWith(GlobalsFileName, StringComparison.OrdinalIgnoreCase)
                select fileName;
        }

        #region Initialization

        private static ILogger InitializeLogging()
        {
            Reusable.Logging.NLog.Tools.LayoutRenderers.InvariantPropertiesLayoutRenderer.Register();

            Reusable.Logging.Logger.ComputedProperties.Add(new Reusable.Logging.ComputedProperties.AppSetting(name: "Environment", key: $"Gunter.Program.Config.Environment"));
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

        private static async Task<IContainer> InitializeContainer()
        {
            try
            {
                var containerBuilder = new ContainerBuilder();

                var variableBuilderTask = Task.Run(() =>
                    new VariableBuilder()
                        .AddVariables<TestFile>(
                            x => x.FullName,
                            x => x.Name)
                        .AddVariables<TestCase>(
                            x => x.Severity,
                            x => x.Message));

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

                containerBuilder
                    .RegisterInstance(await variableBuilderTask)
                    .As<IVariableBuilder>();

                LogEntry.New().Debug().Message("IoC initialized.").Log(Logger);

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
                    VariableValidator.ValidateNamesNotReserved(globals);
                }

                LogEntry.New().Debug().Message("Globals initialized.").Log(Logger);

                return globals.Add(VariableName.Environment, Configuration.Load<Program, ProgramConfig>().Environment);
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
            LogEntry.New().Debug().Message("Initializing tests...").Log(Logger);

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
                    testFile.FullName = fileName;

                    VariableValidator.ValidateNamesNotReserved(VariableResolver.Empty.MergeWith(testFile.Locals));

                    logEntry.Message($"Test initialized: {fileName}");
                    return testFile;
                }
                catch (Exception ex)
                {
                    logEntry.Error().Message($"Could not initialize test: {fileName}").Exception(ex);
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

    internal static class ExitCode
    {
        public const int Success = 0;
        public const int InitializationError = 1;
        public const int RuntimeError = 2;
    }
}
