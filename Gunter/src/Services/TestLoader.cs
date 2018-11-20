using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gunter.Data;
using JetBrains.Annotations;
using Reusable.IO;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter.Services
{
    internal interface ITestLoader
    {
        IEnumerable<TestBundle> LoadTests(string path);
    }

    [UsedImplicitly]
    internal class TestLoader : ITestLoader
    {
        private readonly ILogger _logger;
        private readonly IDirectoryTree _directoryTree;
        private readonly IFileProvider _fileProvider;
        private readonly ITestFileSerializer _testFileSerializer;

        public TestLoader
        (
            ILogger<TestLoader> logger,
            IDirectoryTree directoryTree,
            IFileProvider fileProvider,
            ITestFileSerializer testFileSerializer
        )
        {
            _logger = logger;
            _directoryTree = directoryTree;
            _fileProvider = fileProvider;
            _testFileSerializer = testFileSerializer;
        }

        public IEnumerable<TestBundle> LoadTests(string path)
        {
            _logger.Log(Abstraction.Layer.IO().Meta(new { TestDirectoryName = path }));

            var testFiles =
                _directoryTree
                    .Walk(path, DirectoryTree.MaxDepth(1), DirectoryTree.IgnoreExceptions)
                    .WhereFiles(@"\.json$")
                    .SelectMany(node => node.FileNames.Select(fileName => Path.Combine(node.DirectoryName, fileName)));            

            foreach (var fileName in testFiles)
            {
                using (_logger.BeginScope().AttachElapsed())
                {
                    if (TryLoadTestFile(fileName, out var testFile) && testFile.Enabled)
                    {
                        yield return testFile;
                    }
                }
            }
        }

        private bool TryLoadTestFile(string fileName, out TestBundle testBundle)
        {
            testBundle = default;
            try
            {
                using (var testFileStream = _fileProvider.GetFileInfoAsync(fileName).GetAwaiter().GetResult().CreateReadStream())
                {
                    testBundle = _testFileSerializer.Deserialize(testFileStream);
                    testBundle.FullName = fileName;
                    _logger.Log(Abstraction.Layer.IO().Routine(nameof(TryLoadTestFile)).Completed(), fileName);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(Abstraction.Layer.IO().Routine(nameof(TryLoadTestFile)).Faulted(), fileName, ex);
                return false;
            }
        }
    }
}