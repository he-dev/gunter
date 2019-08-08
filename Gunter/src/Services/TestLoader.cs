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
using Reusable.OmniLog.Nodes;
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
        private readonly IResourceProvider _resources;
        private readonly ITestFileSerializer _testFileSerializer;

        private static readonly ValidationRuleCollection<TestBundle, object> UniqueMergeableIdsValidator =
            ValidationRuleCollection
                .For<TestBundle>()
                .Accept(b => b.When(x => x.All(mergeables => mergeables.GroupBy(m => m.Id).All(g => g.Count() == 1))).Message("All mergable items must have unique ids."));

        public TestLoader
        (
            ILogger<TestLoader> logger,
            IDirectoryTree directoryTree,
            IResourceProvider resources,
            ITestFileSerializer testFileSerializer
        )
        {
            _logger = logger;
            _directoryTree = directoryTree;
            _resources = resources;
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

            foreach (var fileName in testFiles)
            {
                using (_logger.UseScope(correlationHandle: "TestBundle"))
                using (_logger.UseStopwatch())
                {
                    _logger.Log(Abstraction.Layer.IO().Meta(new { TestBundleFileName = fileName }));
                    var testBundle = await LoadTestAsync(fileName);
                    if (testBundle is null || !testBundle.Enabled)
                    {
                        continue;
                    }


                    testBundle.ValidateWith(UniqueMergeableIdsValidator).ThrowOnFailure();

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
                var file = await _resources.GetFileAsync(fileName, MimeType.Plain);
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