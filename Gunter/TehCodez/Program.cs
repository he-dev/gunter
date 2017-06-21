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
using Reusable.ConfigWhiz.Data.Annotations;
using Reusable.ConfigWhiz.Datastores.AppConfig;
using Reusable.Extensions;
using Reusable.Markup.Html;
using Module = Gunter.Reporting.Module;

namespace Gunter
{
    internal class Program
    {
        private static readonly ILogger Logger;

        static Program()
        {
            Logger = InitializeLogging();
            Configuration = InitializeConfiguration();
        }

        public static Configuration Configuration { get; }

        private static int Main(string[] args)
        {
            var mainLogEntry = 
                LogEntry
                    .New()
                    .MessageBuilder(sb => sb.Append($"*** {TehApplicashun.Name} v{TehApplicashun.Version}"))
                    .Stopwatch(sw => sw.Start());

            try
            {
                var container = InitializeContainer().GetAwaiter().GetResult();
                using (var scope = container.BeginLifetimeScope())
                {
                    var tehApp = scope.Resolve<TehApplicashun>();
                    tehApp.Start(args);                    
                }

                mainLogEntry.Info().MessageBuilder(sb => sb.Append("completed."));
                return 0;
            }
            catch (Exception ex)
            {
                mainLogEntry.Fatal().MessageBuilder(sb => sb.Append("crashed.")).Exception(ex);
                return 1;
            }
            finally
            {
                mainLogEntry.MessageBuilder(sb => sb.Append(" ***")).Log(Logger);
            }
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
                    .RegisterType<PathResolver>()
                    .As<IPathResolver>();

                containerBuilder
                    .RegisterType<CssInliner>();
                //.As<ICssInliner>();

                containerBuilder
                    .RegisterType<SimpleCssParser>()
                    .As<ICssParser>();

                containerBuilder
                    .Register<Func<string, Css>>(c =>
                    {
                        var context = c.Resolve<IComponentContext>();

                        return cssFileName =>
                        {
                            cssFileName = Path.Combine(context.Resolve<Workspace>().Themes, cssFileName);
                            var cssFullName = context.Resolve<IPathResolver>().ResolveFilePath(cssFileName);
                            var fileSystem = context.Resolve<IFileSystem>();
                            var css = context.Resolve<ICssParser>().Parse(fileSystem.ReadAllText(cssFullName));
                            return css;
                        };
                    });

                containerBuilder
                    .Register(c =>
                    {
                        var context = c.Resolve<IComponentContext>();
                        return new AutofacContractResolver(context);
                    }).SingleInstance();

                containerBuilder
                    .RegisterType<FileSystem>()
                    .As<IFileSystem>();

                containerBuilder
                    .RegisterInstance(Configuration.Load<TehApplicashun, Workspace>());

                containerBuilder
                    .RegisterType<TehApplicashun>()
                    .WithParameter(new TypedParameter(typeof(ILogger), LoggerFactory.CreateLogger(nameof(TehApplicashun))))
                    .PropertiesAutowired();
                    //.WithProperty(nameof(TehApplicashun.Workspace));//, Configuration.Load<TehApplicashun, Workspace>());

                LogEntry.New().Debug().Message("IoC initialized.").Log(Logger);

                return containerBuilder.Build();
            }
            catch (Exception ex)
            {
                throw new InitializationException("Could not initialize container.", ex);
            }
        }
       
        #endregion
    }

    [SettingName("TehApp")]
    internal class TehApplicashun
    {
        public static readonly string Name = Assembly.GetAssembly(typeof(Program)).GetName().Name;
        public static readonly string Version = "2.0.0";
        private static readonly string GlobalFileName = "_Global.json";

        private readonly ILogger _logger;
        private readonly IPathResolver _pathResolver;
        private readonly IFileSystem _fileSystem;
        private readonly IVariableBuilder _variableBuilder;
        private readonly AutofacContractResolver _autofacContractResolver;
        private readonly TestRunner _testRunner;

        public TehApplicashun(
            ILogger logger,
            IPathResolver pathResolver,
            IFileSystem fileSystem,
            IVariableBuilder variableBuilder,
            AutofacContractResolver autofacContractResolver,
            TestRunner testRunner)
        {
            _logger = logger;
            _pathResolver = pathResolver;
            _fileSystem = fileSystem;
            _variableBuilder = variableBuilder;
            _autofacContractResolver = autofacContractResolver;
            _testRunner = testRunner;
        }

        public Workspace Workspace { get; set; }

        public void Start(string[] args)
        {
            var globalFile = LoadGlobalFile();

            var globals = VariableResolver.Empty
                .MergeWith(globalFile.Globals)
                .MergeWith(_variableBuilder.BuildVariables(Workspace));

            var testFiles = LoadTestFiles().ToList();

            LogEntry.New().Debug().Message($"Test files ({testFiles.Count}) loaded.").Log(_logger);
            LogEntry.New().Info().Message($"*** {Name} v{Version} started. ***").Log(_logger);

            _testRunner.RunTestFiles(testFiles, args, globals);
        }

        private GlobalFile LoadGlobalFile()
        {
            var targetsDirectoryName = _pathResolver.ResolveDirectoryPath(Workspace.Targets);
            var fileName = Path.Combine(targetsDirectoryName, GlobalFileName);

            if (!File.Exists(fileName)) { return new GlobalFile(); }

            try
            {
                var globalFileJson = _fileSystem.ReadAllText(fileName);
                var globalFile = JsonConvert.DeserializeObject<GlobalFile>(globalFileJson);

                VariableValidator.ValidateNamesNotReserved(globalFile.Globals, _variableBuilder.Names);

                LogEntry.New().Debug().Message($"{Path.GetFileName(fileName)} loaded.").Log(_logger);

                return globalFile;
            }
            catch (Exception ex)
            {
                throw new InitializationException($"Could not load {Path.GetFileName(fileName)}.", ex);
            }
        }

        [NotNull, ItemNotNull]
        private IEnumerable<TestFile> LoadTestFiles()
        {
            LogEntry.New().Debug().Message("Initializing tests...").Log(_logger);

            return 
                GetTestFileNames()
                    .Select(LoadTest)
                    .Where(Conditional.IsNotNull);
        }

        [NotNull, ItemNotNull]
        private IEnumerable<string> GetTestFileNames()
        {
            var targetsDirectoryName = _pathResolver.ResolveDirectoryPath(Workspace.Targets);

            return
                from fullName in _fileSystem.GetFiles(targetsDirectoryName, "*.json")
                where !Path.GetFileName(fullName).StartsWith("_", StringComparison.OrdinalIgnoreCase)
                select fullName;
        }

        [CanBeNull]
        private TestFile LoadTest(string fileName)
        {
            var logEntry = LogEntry.New().Info();
            try
            {
                var json = _fileSystem.ReadAllText(fileName);
                var testFile = JsonConvert.DeserializeObject<TestFile>(json, new JsonSerializerSettings
                {
                    ContractResolver = _autofacContractResolver,
                    DefaultValueHandling = DefaultValueHandling.Populate,
                    TypeNameHandling = TypeNameHandling.Auto,
                });
                testFile.FullName = fileName;

                VariableValidator.ValidateNamesNotReserved(testFile.Locals, _variableBuilder.Names);

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
                logEntry.Log(_logger);
            }
        }
    }

    [PublicAPI]
    internal interface IFileSystem
    {
        string ReadAllText(string fileName);
        string[] GetFiles(string path, string searchPattern);
    }

    internal class FileSystem : IFileSystem
    {
        public string ReadAllText(string fileName)
        {
            return File.ReadAllText(fileName);
        }

        public string[] GetFiles(string path, string searchPattern)
        {
            return Directory.GetFiles(path, searchPattern);
        }
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
