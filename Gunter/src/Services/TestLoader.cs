using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Custom;
using System.Threading.Tasks;
using Gunter.Data;
using JetBrains.Annotations;
using Reusable;
using Reusable.Extensions;
using Reusable.IO;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Nodes;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.Translucent;
using Reusable.Utilities.JsonNet;

namespace Gunter.Services
{
    internal interface ITestLoader
    {
        IAsyncEnumerable<TestFile> LoadTestsAsync(string testDirectoryName, List<string> includeFileNames);
    }

    [UsedImplicitly]
    internal class TestLoader : ITestLoader
    {
        private readonly ILogger _logger;
        private readonly IDirectoryTree _directoryTree;
        private readonly IResource _resource;
        private readonly IPrettyJsonSerializer _testFileSerializer;

        public TestLoader
        (
            ILogger<TestLoader> logger,
            IDirectoryTree directoryTree,
            IResource resource,
            IPrettyJsonSerializer testFileSerializer
        )
        {
            _logger = logger;
            _directoryTree = directoryTree;
            _resource = resource;
            _testFileSerializer = testFileSerializer;
        }

        public async IAsyncEnumerable<TestFile> LoadTestsAsync(string testDirectoryName, List<string> includeFileNames)
        {
            _logger.Log(Abstraction.Layer.IO().Meta(new { TestDirectoryName = testDirectoryName }));

            var testFiles =
                _directoryTree
                    .Walk(testDirectoryName, DirectoryTreePredicates.MaxDepth(1), PhysicalDirectoryTree.IgnoreExceptions)
                    .WhereFiles(@"\.json$")
                    .FullNames();

            //var testBundles = new List<TestBundle>();

            foreach (var fullName in testFiles)
            {
                using var _ = _logger.BeginScope().WithCorrelationHandle("LoadTestFile").UseStopwatch();

                _logger.Log(Abstraction.Layer.IO().Meta(new { TestFileName = fullName }));

                var isPartial = Path.GetFileName(fullName).StartsWith(TestFile.TemplatePrefix);
                var canLoad = isPartial || includeFileNames.EmptyOr(x => Path.GetFileNameWithoutExtension(fullName).In(x, SoftString.Comparer));

                if (!canLoad)
                {
                    _logger.Log(Abstraction.Layer.IO().Flow().Decision("Skip test file.").Because("Excluded by filter."));
                    continue;
                }

                if (await LoadTestAsync(fullName) is {} testBundle)
                {
                    yield return testBundle;
                }
            }
        }

        private async Task<TestFile?> LoadTestAsync(string fullName)
        {
            try
            {
                var file = await _resource.ReadTextFileAsync(fullName);
                var testBundle = _testFileSerializer.Deserialize<TestFile>(file, TypeDictionary.From(TestFile.SectionTypes)).Pipe(x => x.FullName = fullName);

                if (testBundle.Enabled)
                {
                    var duplicateIds =
                        from section in testBundle
                        from item in section
                        group item by item.Id into g
                        where g.Count() > 1
                        select g;

                    duplicateIds = duplicateIds.ToList();
                    if (duplicateIds.Any())
                    {
                        _logger.Log(Abstraction.Layer.IO().Flow().Decision("Skip test file.").Because("It contains duplicate ids."));
                        _logger.Log(Abstraction.Layer.IO().Meta(duplicateIds.Select(g => g.Key.ToString()), "DuplicateIds").Error());
                    }
                    else
                    {
                        return testBundle;
                    }
                }
                else
                {
                    _logger.Log(Abstraction.Layer.IO().Flow().Decision("Skip test file.").Because("It's disabled."));
                }
            }
            catch (Exception inner)
            {
                _logger.Log(Abstraction.Layer.IO().Routine("LoadTestFile").Faulted(inner));
            }
            finally
            {
                _logger.Log(Abstraction.Layer.IO().Routine("LoadTestFile").Completed());
            }

            return default;
        }
    }
}