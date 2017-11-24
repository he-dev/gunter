using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gunter.Data;
using Gunter.Services.Validators;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable.OmniLog;

namespace Gunter.Services
{
    internal interface ITestLoader
    {
        IEnumerable<TestFile> LoadTests(string path);
    }

    [UsedImplicitly]
    internal class TestLoader : ITestLoader
    {
        private readonly ILogger _logger;
        private readonly IPathResolver _pathResolver;
        private readonly IFileSystem _fileSystem;
        private readonly AutofacContractResolver _autofacContractResolver;

        public TestLoader(
            ILoggerFactory loggerFactory,
            IPathResolver pathResolver,
            IFileSystem fileSystem,
            AutofacContractResolver autofacContractResolver)
        {
            _logger = loggerFactory.CreateLogger(nameof(TestLoader));
            _pathResolver = pathResolver;
            _fileSystem = fileSystem;
            _autofacContractResolver = autofacContractResolver;
        }

        public string GlobalTestFileName { get; set; } = "_Global.json";

        public IEnumerable<TestFile> LoadTests(string path)
        {
            var testFiles = LoadTestFiles(path).ToLookup(IsTestFile);

            var global = testFiles[false].SingleOrDefault() ?? new TestFile();

            foreach (var testFile in testFiles[true])
            {
                MergeGlobals(testFile.Locals, global.Locals);
                yield return testFile;
            }

            bool IsTestFile(TestFile testFile)
            {
                return !Path.GetFileName(testFile.FileName).Equals(GlobalTestFileName, StringComparison.OrdinalIgnoreCase);
            }
        }

        private IEnumerable<TestFile> LoadTestFiles(string path)
        {
            var fullPath = _pathResolver.ResolveDirectoryPath(path);
            var jsonFiles = _fileSystem.GetFiles(fullPath, "*.json");

            foreach (var jsonFile in jsonFiles)
            {
                if (TryLoadTestFile(jsonFile, out var testFile))
                {
                    yield return testFile;
                }
            }
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
                    ObjectCreationHandling = ObjectCreationHandling.Reuse
                });
                testFile.FullName = fileName;

                //VariableValidator.ValidateNamesNotReserved(testFile.Locals, testFile.Locals.Select(x => x.Key));

                return true;
            }
            catch (Exception ex)
            {
                testFile = null;
                return false;
            }
        }

        private static void MergeGlobals(IDictionary<string, object> locals, IDictionary<string, object> globals)
        {
            foreach (var global in globals)
            {
                if (!locals.ContainsKey(global.Key))
                {
                    locals[global.Key] = global.Value;
                }
            }

            //var localVariables =
            //    globalTestFile.Locals.Concat(testFile.Locals)
            //        .GroupBy(x => x.Key)
            //        .Select(g => g.Last())
            //        .ToDictionary(x => x.Key, x => x.Value);
        }
    }
}