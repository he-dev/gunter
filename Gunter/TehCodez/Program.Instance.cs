using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using Reusable.Logging;
using Reusable;
using Gunter.Data;
using System.Threading.Tasks;
using Gunter.AutofacModules;
using Gunter.Messaging.Email;
using Gunter.Messaging.Email.ModuleRenderers;
using Gunter.Reporting;
using Gunter.Reporting.Modules;
using Gunter.Services;
using Gunter.Services.Validators;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NLog.Fluent;
using Reusable.Extensions;
using Reusable.Logging.Loggex;
using Reusable.Markup.Html;
using Module = Gunter.Reporting.Module;

namespace Gunter
{
    internal partial class Program
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

        public Program(
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

            _logger.Log(e => e.Debug().Message($"Test files ({testFiles.Count}) loaded."));
            _logger.Log(e => e.Message($"*** {Name} v{Version} started. ***"));

            _testRunner.RunTestFiles(testFiles, args, globals);
        }

        private GlobalFile LoadGlobalFile()
        {
            var targetsDirectoryName = _pathResolver.ResolveDirectoryPath(Workspace.Targets);
            var fileName = Path.Combine(targetsDirectoryName, GlobalFileName);

            // If there is no _Globa.json then use an empty one but if there is one then it needs to be valid.

            if (!_fileSystem.Exists(fileName))
            {
                return new GlobalFile();
            }

            try
            {
                var globalFileJson = _fileSystem.ReadAllText(fileName);
                var globalFile = JsonConvert.DeserializeObject<GlobalFile>(globalFileJson);

                VariableValidator.ValidateNamesNotReserved(globalFile.Globals, _variableBuilder.Names);

                _logger.Log(e => e.Debug().Message($"{Path.GetFileName(fileName)} loaded."));

                return globalFile;
            }
            catch (Exception ex)
            {
                throw new TestConfigurationException($"Could not load {Path.GetFileName(fileName)}.", ex);
            }
        }

        [NotNull, ItemNotNull]
        private IEnumerable<TestFile> LoadTestFiles()
        {
            _logger.Log(e => e.Debug().Message("Initializing tests..."));

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
            var logger = _logger.BeginLog();
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

                logger.LogEntry.Message($"Test initialized: {fileName}");
                return testFile;
            }
            catch (Exception ex)
            {
                logger.LogEntry.Error().Message($"Could not initialize test: {fileName}").Exception(ex);
                return null;
            }
            finally
            {
                logger.EndLog();
            }
        }
    }

    public class TestConfigurationException : Exception
    {
        public TestConfigurationException(string message, Exception innerException):base(message, innerException) { }
    }
}
