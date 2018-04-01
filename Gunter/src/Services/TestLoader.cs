using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gunter.Data;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.Reflection;

namespace Gunter
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
        private readonly TestFileSerializer _testFileSerializer;

        public TestLoader(
            ILogger<TestLoader> logger,
            ITestFileProvider testFileProvider,
            TestFileSerializer testFileSerializer)
        {
            _logger = logger;
            _testFileProvider = testFileProvider;
            _testFileSerializer = testFileSerializer;
        }

        public IEnumerable<TestBundle> LoadTests(string path)
        {
            _logger.Log(Abstraction.Layer.IO().Data().Argument(new { path }));

            foreach (var testFileInfo in _testFileProvider.EnumerateTestFiles(path))
            {
                if (TryLoadTestFile(testFileInfo, out var testFile))
                {
                    yield return testFile;
                }
            }
        }

        private bool TryLoadTestFile(TestFileInfo testFileInfo, out TestBundle testBundle)
        {
            _logger.Log(Abstraction.Layer.IO().Data().Argument(new { testFileInfo = new { testFileInfo.Name } }));

            testBundle = default;
            try
            {
                using (var testFileStream = testFileInfo.CreateReadStream())
                {
                    testBundle = _testFileSerializer.Deserialize(testFileStream);
                    testBundle.FullName = testFileInfo.Name;
                    _logger.Log(Abstraction.Layer.IO().Action().Finished(nameof(TryLoadTestFile)));
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(Abstraction.Layer.IO().Action().Failed(nameof(TryLoadTestFile)), ex);
                return false;
            }
        }
    }

    internal class TestFileSerializer
    {
        private readonly Newtonsoft.Json.JsonSerializer _jsonSerializer;

        public TestFileSerializer(IContractResolver contractResolver)
        {
            _jsonSerializer = new Newtonsoft.Json.JsonSerializer
            {
                ContractResolver = contractResolver,
                DefaultValueHandling = DefaultValueHandling.Populate,
                TypeNameHandling = TypeNameHandling.Auto,
                ObjectCreationHandling = ObjectCreationHandling.Reuse,
            };
        }

        public TestBundle Deserialize(Stream testFileStream)
        {
            using (var streamReader = new StreamReader(testFileStream))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                return _jsonSerializer.Deserialize<TestBundle>(jsonReader);
            }
        }
    }

    internal interface ITestFileProvider
    {
        IEnumerable<TestFileInfo> EnumerateTestFiles(string path);
    }

    internal class TestFileProvider : ITestFileProvider
    {
        public IEnumerable<TestFileInfo> EnumerateTestFiles(string path)
        {
            return
                Directory
                    .EnumerateFiles(path, "*.json")
                    .Select(testFileName => new TestFileInfo
                    {
                        Name = testFileName,
                        CreateReadStream = () => File.OpenRead(testFileName)
                    });
        }
    }

    internal class TestFileInfo
    {
        public string Name { get; set; }

        public Func<Stream> CreateReadStream { get; set; }
    }

    public enum MergeMode
    {
        Base,
        Join
    }

    internal class MergableAttribute : Attribute { }
}