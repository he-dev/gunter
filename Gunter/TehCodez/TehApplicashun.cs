using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Gunter.Data;
using Gunter.Services;
using Gunter.Services.Validators;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable.ConfigWhiz.Data.Annotations;
using Reusable.Extensions;
using Reusable.Logging;

namespace Gunter
{
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
}