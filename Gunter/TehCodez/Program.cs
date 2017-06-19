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
using Autofac.Extras.AggregateService;
using Gunter.Data.Configuration;
using Gunter.Messaging.Email;
using Gunter.Messaging.Email.ModuleRenderers;
using Gunter.Reporting;
using Gunter.Reporting.Modules;
using Gunter.Services;
using Gunter.Services.Validators;
using JetBrains.Annotations;
using NLog.Fluent;
using Reusable.ConfigWhiz;
using Reusable.ConfigWhiz.Datastores.AppConfig;
using Reusable.Extensions;
using Reusable.Markup.Html;
using Module = Gunter.Reporting.Module;

namespace Gunter
{
    internal class Program
    {
        public static readonly string Name = Assembly.GetAssembly(typeof(Program)).GetName().Name;
        public static readonly string Version = "2.0.0";

        private static readonly ILogger Logger;

        static Program()
        {
            Logger = InitializeLogging();
            Configuration = InitializeConfiguration();
        }

        public static readonly string GlobalsFileName = "_Globals.json";

        public static Configuration Configuration { get; }

        // We need this before the IoC is created.
        public static readonly IPathResolver PathResolver = new PathResolver();

        private static int Main(string[] args)
        {
            try
            {
                var container = InitializeContainer().GetAwaiter().GetResult();
                using (var scope = container.BeginLifetimeScope())
                {
                    var testDirectoryName = PathResolver.ResolveDirectoryPath(Configuration.Load<Program, Workspace>().Targets);

                    var globals = InitializeGlobals(testDirectoryName, GlobalsFileName, container.Resolve<IVariableBuilder>());
                    var testFileNames = GetTestFileNames(testDirectoryName);
                    var testFiles = InitializeTests(testFileNames, container, container.Resolve<IVariableBuilder>().Names).ToList();

                    LogEntry.New().Info().Message($"*** {Name} v{Version} started. ***").Log(Logger);

                    scope.Resolve<TestRunner>().RunTestFiles(testFiles, args, globals);
                }

                return 0;
            }
            // Exception should already be logged elsewhere and rethrown to exit the application.
            catch (Exception ex)
            {
                LogEntry.New().Fatal().Message($"*** {Name} v{Version} crashed. ***").Exception(ex).Log(Logger);
                return 1;
            }
            finally
            {
                LogEntry.New().Info().Message($"*** {Name} v{Version} exited. ***").Log(Logger);
            }
        }

        private static IEnumerable<string> GetTestFileNames(string directoryName)
        {
            return
                from fullName in Directory.GetFiles(directoryName, "*.json")
                where !Path.GetFileName(fullName).StartsWith("_", StringComparison.OrdinalIgnoreCase)
                select fullName;
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
                            x => x.FileName)
                        .AddVariables<IDataSource>(
                            x => x.Elapsed)
                        .AddVariables<TestCase>(
                            x => x.Severity,
                            x => x.Message,
                            x => x.Elapsed)
                        .AddVariables<Workspace>(
                            x => x.Environment,
                            x => x.AppName));

                containerBuilder
                    .RegisterType<TestRunner>()
                    .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(TestRunner))));

                containerBuilder
                    .RegisterType<Data.SqlClient.TableOrView>()
                    .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(Data.SqlClient.TableOrView))));

                containerBuilder
                    .RegisterType<HtmlEmail>()
                    .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(HtmlEmail))));

                #region Initialize reporting modules

                containerBuilder
                    .RegisterType<Report>()
                    .As<IReport>();

                containerBuilder
                    .RegisterType<TestCaseInfo>();

                containerBuilder
                    .RegisterType<DataSourceInfo>();

                containerBuilder
                    .RegisterType<DataSummary>();

                #endregion

                #region Initialize renderers

                containerBuilder
                    .RegisterType<GreetingRenderer>()
                    .As<ModuleRenderer>();

                containerBuilder
                    .RegisterType<TableRenderer>()
                    .As<ModuleRenderer>();

                containerBuilder
                    .RegisterType<SignatureRenderer>()
                    .As<ModuleRenderer>();
                
                #endregion

                containerBuilder
                    .RegisterInstance(await variableBuilderTask)
                    .As<IVariableBuilder>();

                containerBuilder
                    .RegisterInstance(PathResolver)
                    .As<IPathResolver>();

                containerBuilder
                    .RegisterType<SimpleCssParser>()
                    .As<ICssParser>();

                containerBuilder
                    .Register<Func<string, StyleVisitor>>(c =>
                    {
                        var context = c.Resolve<IComponentContext>();

                        return cssFileName =>
                        {
                            cssFileName = Path.Combine(Configuration.Load<Program, Workspace>().Themes, cssFileName);
                            var cssFullName = context.Resolve<IPathResolver>().ResolveFilePath(cssFileName);
                            var css = context.Resolve<ICssParser>().Parse(File.ReadAllText(cssFullName));
                            return new StyleVisitor((Dictionary<string, string>)css);
                        };
                    });

                LogEntry.New().Debug().Message("IoC initialized.").Log(Logger);

                return containerBuilder.Build();
            }
            catch (Exception ex)
            {
                throw new InitializationException("Could not initialize container.", ex);
            }
        }

        private static IVariableResolver InitializeGlobals(string directoryName, string fileName, IVariableBuilder variableBuilder)
        {
            var fullName = Path.Combine(directoryName, fileName);

            if (!File.Exists(fullName)) { return VariableResolver.Empty; }

            try
            {
                var globalsFile = File.ReadAllText(fullName);
                var globals = JsonConvert.DeserializeObject<Dictionary<string, object>>(globalsFile);
                VariableValidator.ValidateNamesNotReserved(globals, variableBuilder.Names);
                var variableResolver = VariableResolver.Empty
                    .MergeWith(globals)
                    .MergeWith(variableBuilder.BuildVariables(Configuration.Load<Program, Workspace>()));

                LogEntry.New().Debug().Message("Globals initialized.").Log(Logger);

                return variableResolver;
            }
            catch (Exception ex)
            {
                throw new InitializationException("Could not initialize globals.", ex);
            }
        }

        [NotNull]
        [ItemNotNull]
        private static IEnumerable<TestFile> InitializeTests(IEnumerable<string> fileNames, IContainer container, IEnumerable<string> reservedNames)
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
                        TypeNameHandling = TypeNameHandling.Auto,
                        //NullValueHandling = NullValueHandling.Ignore
                    });
                    testFile.FullName = fileName;

                    VariableValidator.ValidateNamesNotReserved(testFile.Locals, reservedNames);

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
