using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Data;
using JetBrains.Annotations;
using Reusable;
using Reusable.Collections;
using Reusable.Flawless;
using Reusable.IOnymous;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter.Services
{
    internal interface ITestLoader
    {
        [ItemNotNull]
        Task<IList<TestBundle>> LoadTestsAsync(string testDirectoryName);
    }

    [UsedImplicitly]
    internal class TestLoader : ITestLoader
    {
        private readonly ILogger _logger;
        private readonly IDirectoryTree _directoryTree;
        private readonly IResourceProvider _resourceProvider;
        private readonly ITestFileSerializer _testFileSerializer;

        private static readonly IExpressValidator<TestBundle> UniqueMergeableIdsValidator = ExpressValidator.For<TestBundle>(builder =>
        {
            builder.True(
                testBundle => testBundle.All(
                    mergeables => mergeables.GroupBy(m => m.Id).All(g => g.Count() == 1)));
        });

        public TestLoader
        (
            ILogger<TestLoader> logger,
            IDirectoryTree directoryTree,
            IResourceProvider resourceProvider,
            ITestFileSerializer testFileSerializer
        )
        {
            _logger = logger;
            _directoryTree = directoryTree;
            _resourceProvider = resourceProvider;
            _testFileSerializer = testFileSerializer;
        }

        public async Task<IList<TestBundle>> LoadTestsAsync(string testDirectoryName)
        {
            _logger.Log(Abstraction.Layer.IO().Meta(new { TestDirectoryName = testDirectoryName }));

            var testFiles =
                _directoryTree
                    .Walk(testDirectoryName, PhysicalDirectoryTree.MaxDepth(1), PhysicalDirectoryTree.IgnoreExceptions)
                    .WhereFiles(@"\.json$")
                    .SelectMany(node => node.FileNames.Select(fileName => Path.Combine(node.DirectoryName, fileName)));

            var testBundles = new List<TestBundle>();

            foreach (var fileName in testFiles) //.Where(fileName => tests is null || tests.Contains(Path.GetFileNameWithoutExtension(fileName), StringComparer.OrdinalIgnoreCase)))
            {
                using (_logger.BeginScope().WithCorrelationHandle("TestBundle").AttachElapsed())
                {
                    _logger.Log(Abstraction.Layer.IO().Meta(new { TestBundleFileName = fileName }));
                    var testBundle = await LoadTestAsync(fileName);
                    if (testBundle is null || !testBundle.Enabled)
                    {
                        continue;
                    }


                    UniqueMergeableIdsValidator.Validate(testBundle).Assert();

                    testBundles.Add(testBundle);
                }
            }

            return testBundles;
        }

        [ItemCanBeNull]
        private async Task<TestBundle> LoadTestAsync(string fileName)
        {
            try
            {
                var file = await _resourceProvider.GetFileAsync(fileName, MimeType.Text);
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    var testBundle = await _testFileSerializer.DeserializeAsync(memoryStream);
                    testBundle.FullName = fileName;
                    _logger.Log(Abstraction.Layer.IO().Routine(nameof(LoadTestAsync)).Completed());
                    return testBundle;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(Abstraction.Layer.IO().Routine(nameof(LoadTestAsync)).Faulted(), ex);
                return default;
            }
        }
    }
}