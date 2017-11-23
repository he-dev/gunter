using System;
using System.Collections.Generic;
using System.IO;
using Gunter.Data;
using Gunter.Services.Validators;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable.OmniLog;

namespace Gunter.Services
{
    internal interface ITestLoader
    {
        (TestFile GlobalTestFile, IList<TestFile> TestFiles) LoadTests(string path);
    }

    [UsedImplicitly]
    internal class TestLoader
    {
        private readonly ILogger _logger;
        private readonly IPathResolver _pathResolver;
        private readonly IVariableContainer _variableContainer;
        private readonly IFileSystem _fileSystem;
        private readonly AutofacContractResolver _autofacContractResolver;

        public TestLoader(
            ILoggerFactory loggerFactory, 
            IPathResolver pathResolver, 
            IVariableContainer variableContainer, 
            IFileSystem fileSystem, 
            AutofacContractResolver autofacContractResolver)
        {
            _logger = loggerFactory.CreateLogger(nameof(TestLoader));
            _pathResolver = pathResolver;
            _variableContainer = variableContainer;
            _fileSystem = fileSystem;
            _autofacContractResolver = autofacContractResolver;
        }

        public string GlobalTestFileName { get; set; } = "_Global.json";

        public (TestFile GlobalTestFile, IList<TestFile> TestFiles) LoadTests(string path)
        {
            var fullPath = _pathResolver.ResolveDirectoryPath(path);
            var jsonFiles = _fileSystem.GetFiles(fullPath, "*.json");

            var globalTestFile = new TestFile();
            var testFiles = new List<TestFile>();

            foreach (var jsonFile in jsonFiles)
            {
                if (TryLoadTestFile(jsonFile, out var testFile))
                {
                    var isGlobalTestFile = Path.GetFileName(jsonFile).Equals(GlobalTestFileName, StringComparison.OrdinalIgnoreCase);
                    if (isGlobalTestFile)
                    {
                        globalTestFile = testFile;
                    }
                    else
                    {
                        testFiles.Add(testFile);
                    }
                }
            }

            return (globalTestFile, testFiles);
        }

        private bool TryLoadTestFile(string fileName, out TestFile testFile)
        {
            try
            {
                var json = _fileSystem.ReadAllText(fileName);
                testFile = JsonConvert.DeserializeObject<TestFile>(json, new JsonSerializerSettings
                {
                    ContractResolver = _autofacContractResolver,
                    DefaultValueHandling = DefaultValueHandling.Populate,
                    TypeNameHandling = TypeNameHandling.Auto,
                });
                testFile.FullName = fileName;

                VariableValidator.ValidateNamesNotReserved(testFile.Locals, _variableContainer.Names);

                return true;
            }
            catch (Exception ex)
            {
                testFile = null;
                return false;
            }
        }
    }
}