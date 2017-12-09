﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gunter.Data;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;
using Reusable.IO;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.SmartConfig;

namespace Gunter
{
    [UsedImplicitly]
    internal class TestLoader : ITestLoader
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IVariableValidator _variableValidator;
        private readonly AutofacContractResolver _autofacContractResolver;
        private readonly IEnumerable<string> _lookupPaths;

        public TestLoader(
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IFileSystem fileSystem,
            IVariableValidator variableValidator,
            AutofacContractResolver autofacContractResolver)
        {
            _logger = loggerFactory.CreateLogger(nameof(TestLoader));
            _fileSystem = fileSystem;
            _variableValidator = variableValidator;
            _autofacContractResolver = autofacContractResolver;
            _lookupPaths = configuration.Select<List<string>>("LookupPaths");
        }

        public string GlobalTestFileName { get; set; } = "_Global.json";

        public IEnumerable<TestFile> LoadTests(string path)
        {
            _logger.Log(Category.Snapshot.Arguments(new { path }), Layer.IO);

            var testFiles = LoadTestFiles(path).ToLookup(IsTestFile);

            var global = testFiles[false].SingleOrDefault() ?? new TestFile();

            foreach (var testFile in testFiles[true])
            {
                testFile.Locals = MergeVariables(global.Locals, testFile.Locals);
                testFile.DataSources = testFile.DataSources.Concat(global.DataSources).ToList();
                testFile.Messages = testFile.Messages.Concat(global.Messages).ToList();
                testFile.Reports = testFile.Reports.Concat(global.Reports).ToList();
                yield return testFile;
            }

            bool IsTestFile(TestFile testFile)
            {
                return !Path.GetFileName(testFile.FileName).Equals(GlobalTestFileName, StringComparison.OrdinalIgnoreCase);
            }
        }

        private IEnumerable<TestFile> LoadTestFiles(string path)
        {
            var testFilesDirectoryName = _fileSystem.FindDirectory(path, _lookupPaths);
            var jsonFiles = _fileSystem.EnumerateFiles(testFilesDirectoryName).Where(FileFilterFactory.Default.Create(".json"));

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
            _logger.Log(Category.Snapshot.Arguments(new { fileName }), Layer.IO);

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

                _variableValidator.ValidateNamesNotReserved(testFile.Locals);

                _logger.Log(Category.Action.Finished(nameof(TryLoadTestFile)), Layer.IO);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(Category.Action.Failed(nameof(TryLoadTestFile), ex), Layer.IO);
                testFile = null;
                return false;
            }
        }

        private static Dictionary<SoftString, object> MergeVariables(IDictionary<SoftString, object> globals, IDictionary<SoftString, object> locals)
        {
            return
                globals.Concat(locals)
                    .GroupBy(x => x.Key)
                    .Select(g => g.Last())
                    .ToDictionary(x => x.Key, x => x.Value);
        }
    }
}