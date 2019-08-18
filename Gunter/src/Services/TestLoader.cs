using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Data;
using JetBrains.Annotations;
using Reusable;
using Reusable.Collections;
using Reusable.Data;
using Reusable.Exceptionize;
using Reusable.Extensions;
using Reusable.Flawless;
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
        [ItemNotNull]
        Task<IList<TestBundle>> LoadTestsAsync(string testDirectoryName, IList<string> includeFileNames);
    }

    [UsedImplicitly]
    internal class TestLoader : ITestLoader
    {
        private readonly ILogger _logger;
        private readonly IDirectoryTree _directoryTree;
        private readonly IResourceRepository _resources;
        private readonly IPrettyJsonSerializer _testFileSerializer;

//        private static readonly ValidationRuleCollection<TestBundle, object> UniqueMergeableIdsValidator =
//            ValidationRuleCollection
//                .For<TestBundle>()
//                .Accept(b => b.When(x => x.All(mergeables => mergeables.GroupBy(m => m.Id).All(g => g.Count() == 1))).Message("All mergable items must have unique ids."));

        public TestLoader
        (
            ILogger<TestLoader> logger,
            IDirectoryTree directoryTree,
            IResourceRepository resources,
            IPrettyJsonSerializer testFileSerializer
        )
        {
            _logger = logger;
            _directoryTree = directoryTree;
            _resources = resources;
            _testFileSerializer = testFileSerializer;
        }

        public async Task<IList<TestBundle>> LoadTestsAsync(string testDirectoryName, IList<string> includeFileNames)
        {
            _logger.Log(Abstraction.Layer.IO().Meta(new { TestDirectoryName = testDirectoryName }));

            var testFiles =
                _directoryTree
                    .Walk(testDirectoryName, PhysicalDirectoryTree.MaxDepth(1), PhysicalDirectoryTree.IgnoreExceptions)
                    .WhereFiles(@"\.json$")
                    .SelectMany(node => node.FileNames.Select(fileName => Path.Combine(node.DirectoryName, fileName)));

            var testBundles = new List<TestBundle>();

            foreach (var fullName in testFiles)
            {
                using (_logger.UseScope(correlationHandle: "LoadTestFile"))
                using (_logger.UseStopwatch())
                {
                    _logger.Log(Abstraction.Layer.IO().Meta(new { TestFileName = fullName }));

                    var fileName = Path.GetFileName(fullName);
                    var isTemplate = fileName.StartsWith("_");

                    var canLoad =
                        isTemplate ||
                        includeFileNames is null ||
                        includeFileNames.Any(includeFileName => SoftString.Comparer.Equals(includeFileName, Path.GetFileNameWithoutExtension(fullName)));

                    if (!canLoad)
                    {
                        _logger.Log(Abstraction.Layer.IO().Flow().Decision("Skip test file.").Because("Excluded by filter."));
                        continue;
                    }

                    try
                    {
                        var file = await _resources.ReadTextFileAsync(fullName);
                        var testBundle = _testFileSerializer.Deserialize<TestBundle>(file, TypeDictionary.From(TestBundle.KnownTypes));

                        if (!testBundle.Enabled)
                        {
                            _logger.Log(Abstraction.Layer.IO().Flow().Decision("Skip test file.").Because("It's disabled."));
                            continue;
                        }

                        var duplicateIds =
                            from mergables in testBundle
                            from mergable in mergables
                            group mergable by mergable.Id into g
                            where g.Count() > 1
                            select g;

                        duplicateIds = duplicateIds.ToList();
                        if (duplicateIds.Any())
                        {
                            _logger.Log(Abstraction.Layer.IO().Meta(duplicateIds.Select(g => g.Key.ToString()), "DuplicateIds").Error());
                            continue;
                        }

                        testBundle.FullName = fullName;
                        testBundles.Add(testBundle);
                        _logger.Log(Abstraction.Layer.IO().Routine("LoadTestFile").Completed());
                    }
                    catch (Exception inner)
                    {
                        _logger.Log(Abstraction.Layer.IO().Routine("LoadTestFile").Faulted(inner));
                    }
                }
            }

            return testBundles;
        }
    }
}