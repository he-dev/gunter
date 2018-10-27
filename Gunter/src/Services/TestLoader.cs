using System;
using System.Collections.Generic;
using Gunter.Data;
using JetBrains.Annotations;
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
        private readonly ITestFileProvider _testFileProvider;
        private readonly ITestFileSerializer _testFileSerializer;

        public TestLoader(
            ILogger<TestLoader> logger,
            ITestFileProvider testFileProvider,
            ITestFileSerializer testFileSerializer)
        {
            _logger = logger;
            _testFileProvider = testFileProvider;
            _testFileSerializer = testFileSerializer;
        }

        public IEnumerable<TestBundle> LoadTests(string path)
        {
            _logger.Log(Abstraction.Layer.IO().Argument(new { path }));

            foreach (var testFileInfo in _testFileProvider.EnumerateTestFiles(path))
            {
                using (_logger.BeginScope())
                {
                    if (TryLoadTestFile(testFileInfo, out var testFile) && testFile.Enabled)
                    {
                        yield return testFile;
                    }
                }
            }
        }

        private bool TryLoadTestFile(TestFileInfo testFileInfo, out TestBundle testBundle)
        {
            _logger.Log(Abstraction.Layer.IO().Argument(new { testFileInfo = new { testFileInfo.Name } }));

            testBundle = default;
            try
            {
                using (var testFileStream = testFileInfo.CreateReadStream())
                {
                    testBundle = _testFileSerializer.Deserialize(testFileStream);
                    testBundle.FullName = testFileInfo.Name;
                    _logger.Log(Abstraction.Layer.IO().Routine(nameof(TryLoadTestFile)).Completed());
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(Abstraction.Layer.IO().Routine(nameof(TryLoadTestFile)).Faulted(), ex);
                return false;
            }
        }
    }
}