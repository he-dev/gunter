using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Custom;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Gunter.Data;
using Gunter.Services;
using Microsoft.Extensions.Caching.Memory;
using Reusable;
using Reusable.Exceptionize;
using Reusable.Extensions;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Annotations;
using Reusable.Flowingo.Steps;
using Reusable.IO;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Nodes;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.Translucent;
using Reusable.Utilities.JsonNet;

namespace Gunter
{
    public class TestFile
    {
        public string Name { get; set; }

        public bool CanLoad { get; set; }

        public bool IsPartial => Path.GetFileName(Name).StartsWith(TestBundle.TemplatePrefix);

        public TestBundleType Type =>
            Name is null
                ? TestBundleType.Unknown
                : Path.GetFileName(Name).StartsWith(TestBundle.TemplatePrefix)
                    ? TestBundleType.Template
                    : TestBundleType.Regular;
    }

    namespace Workflows
    {
        internal class SessionWorkflow : Workflow<SessionContext>
        {
            public static SessionWorkflow Create(IServiceProvider serviceProvider) => new SessionWorkflow
            {
                new FindProjects(serviceProvider),
                new LoadProjects(),
                new RunProjects
                {
                    ForEachProject =
                    {
                        new RunProject
                        {
                            ForEachTest =
                            {
                                new CreateRuntimeProperties(),
                                new GetData(),
                                new FilterData(),
                                new RunTest(),
                                new PublishTestResult()
                            }
                        }
                    }
                }
            };
        }

        internal class SessionContext
        {
            public string TestDirectoryName { get; set; }

            public TestFilter TestFilter { get; set; }

            public List<TestFile> TestFiles { get; set; } = new List<TestFile>();

            public List<TestBundle> TestBundles { get; set; } = new List<TestBundle>();
        }

        public static class Partial
        {
            public static TResult Read<T, TResult>(this T partial, Func<T, TResult> selector, IEnumerable<TestBundle> partials) where T : IPartial
            {
                if (selector(partial) is {} result)
                {
                    return result;
                }
                else
                {
                    if (partials.SingleOrDefault(p => p.Name == partial.Merge.Name) is {} other)
                    {
                        partial = other.Flatten().OfType<T>().SingleOrDefault(p => p.Id == partial.Id);
                    }

                    return selector(partial);
                }
            }
        }

        public class TestFilter
        {
            public List<string> DirectoryNamePatterns { get; set; } = new List<string> { ".+" };
            public List<string> FileNamePatterns { get; set; } = new List<string> { ".+" };
            public List<string> TestNamePatterns { get; set; } = new List<string> { ".+" };
            public List<string> Tags { get; set; } = new List<string>();
        }

        internal class FindProjects : Step<SessionContext>
        {
            public FindProjects(IServiceProvider serviceProvider) : base(serviceProvider) { }

            [Service]
            public ILogger<FindProjects> Logger { get; set; }

            [Service]
            public IDirectoryTree DirectoryTree { get; set; }

            public override async Task ExecuteAsync(SessionContext context)
            {
                context.TestFiles =
                    DirectoryTree
                        .Walk(context.TestDirectoryName, DirectoryTreePredicates.MaxDepth(1), PhysicalDirectoryTree.IgnoreExceptions)
                        .WhereFiles(@"\.json$")
                        // .Where(node =>
                        // {
                        //     if (node.DirectoryName.Matches(context.TestFilter.DirectoryNamePatterns, RegexOptions.IgnoreCase))
                        //     {
                        //         return new DirectoryTreeNodeView();
                        //     }
                        //
                        //     context.TestFilter.DirectoryNamePatterns.Any(p => node.DirectoryName.Matches(p, RegexOptions.IgnoreCase));
                        //     context.TestFilter.FileNamePatterns.w
                        // })
                        .FullNames()
                        .Select(n => new TestFile { Name = n })
                        .ToList();

                await ExecuteNextAsync(context);
            }
        }


        internal class LoadProjects : Step<SessionContext>
        {
            [Service]
            public ILogger<FindProjects> Logger { get; set; }

            [Service]
            public IResource Resource { get; set; }

            [Service]
            public IPrettyJsonSerializer TestFileSerializer { get; set; }

            public override async Task ExecuteAsync(SessionContext context)
            {
                foreach (var testFile in context.TestFiles.Where(tf => tf.IsPartial || tf.CanLoad))
                {
                    //using var _ = _logger.BeginScope().WithCorrelationHandle("LoadTestFile").UseStopwatch();

                    //_logger.Log(Abstraction.Layer.IO().Meta(new { TestFileName = fullName }));

                    if (await LoadTestAsync(testFile.Name) is {} testBundle)
                    {
                        context.TestBundles.Add(testBundle.Pipe(x => { x.TestFile = testFile; }));
                    }
                }
            }

            private async Task<TestBundle?> LoadTestAsync(string fullName)
            {
                try
                {
                    var file = await Resource.ReadTextFileAsync(fullName);
                    var testBundle = TestFileSerializer.Deserialize<TestBundle>(file, TypeDictionary.From(TestBundle.SectionTypes)).Pipe(x => x.FullName = fullName);

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
                            //_logger.Log(Abstraction.Layer.IO().Flow().Decision("Skip test file.").Because("It contains duplicate ids."));
                            //_logger.Log(Abstraction.Layer.IO().Meta(duplicateIds.Select(g => g.Key.ToString()), "DuplicateIds").Error());
                        }
                        else
                        {
                            return testBundle;
                        }
                    }
                    else
                    {
                        //_logger.Log(Abstraction.Layer.IO().Flow().Decision("Skip test file.").Because("It's disabled."));
                    }
                }
                catch (Exception inner)
                {
                    //_logger.Log(Abstraction.Layer.IO().Routine("LoadTestFile").Faulted(inner));
                }
                finally
                {
                    //_logger.Log(Abstraction.Layer.IO().Routine("LoadTestFile").Completed());
                }

                return default;
            }
        }

        internal class ProjectContext
        {
            public List<TestContext> TestContexts { get; set; } = new List<TestContext>();
        }

        internal class TestContext : IDisposable
        {
            public TestBundle TestBundle { get; set; }

            public TestCase TestCase { get; set; }

            public IQuery Query { get; set; }

            public QueryResult? QueryResult { get; set; }

            public TimeSpan GetDataElapsed { get; set; }

            public TimeSpan FilterDataElapsed { get; set; }

            public RuntimePropertyProvider Properties { get; set; }

            public void Dispose() => QueryResult?.Dispose();
        }

        internal class RunProjects : Step<SessionContext>
        {
            [Service]
            public ILogger<FindProjects> Logger { get; set; }

            public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount * 2;

            public Workflow<ProjectContext> ForEachProject { get; set; } = new Workflow<ProjectContext>();

            public override async Task ExecuteAsync(SessionContext context)
            {
                var actions = new ActionBlock<ProjectContext>
                (
                    ForEachProject.ExecuteAsync,
                    new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism }
                );

                var projectGroups =
                    from testBundle in context.TestBundles
                    from testCase in testBundle.Tests
                    from query in testCase.Queries(testBundle)
                    group new TestContext
                    {
                        TestBundle = testBundle,
                        TestCase = testCase,
                        Query = query
                    } by testBundle.Name into projectGroup
                    select projectGroup;

                foreach (var projectGroup in projectGroups)
                {
                    await actions.SendAsync(new ProjectContext
                    {
                        TestContexts = projectGroup.ToList()
                    });
                }

                actions.Complete();
                await actions.Completion;
            }
        }

        internal class RunProject : Step<ProjectContext>
        {
            public Workflow<TestContext> ForEachTest { get; set; } = new Workflow<TestContext>();

            public override Task ExecuteAsync(ProjectContext context)
            {
                throw new NotImplementedException();
            }
        }

        internal class CreateRuntimeProperties : Step<TestContext>
        {
            public override async Task ExecuteAsync(TestContext context)
            {
                var properties = RuntimeProperty.BuiltIn.Enumerate().Concat(context.TestBundle.Variables.Flatten());
                var objects = new object[] { context.TestBundle, context.TestCase };
                context.Properties = new RuntimePropertyProvider(properties.ToImmutableList(), objects.ToImmutableList());

                await ExecuteNextAsync(context);
            }
        }

        internal class GetData : Step<TestContext>
        {
            [Service]
            public ILogger<GetData> Logger { get; set; }

            [Service]
            public IMemoryCache Cache { get; set; }

            public override async Task ExecuteAsync(TestContext context)
            {
                using var scope = Logger.BeginScope().WithCorrelationHandle(nameof(GetData)).UseStopwatch();
                Logger.Log(Abstraction.Layer.Service().Subject(new { QueryId = context.Query.Id }));
                try
                {
                    context.QueryResult = await Cache.GetOrCreateAsync($"{context.TestBundle.Name}.{context.Query.Id}", async entry => await context.Query.ExecuteAsync(context.Properties));
                    context.GetDataElapsed = Logger.Scope().Stopwatch().Elapsed;
                    Logger.Log(Abstraction.Layer.Database().Counter(new { RowCount = context.QueryResult.Data.Rows.Count, ColumnCount = context.QueryResult.Data.Columns.Count }));
                    await ExecuteNextAsync(context);
                }
                catch (Exception inner)
                {
                    throw DynamicException.Create(GetType().ToPrettyString(), $"Error getting or processing data for query '{context.Query.Id}'.", inner);
                }
            }
        }

        internal class FilterData : Step<TestContext>
        {
            [Service]
            public ILogger<FilterData> Logger { get; set; }

            public override async Task ExecuteAsync(TestContext context)
            {
                if (context.Query.Filters is {} filters)
                {
                    using var scope = Logger.BeginScope().WithCorrelationHandle(nameof(FilterData)).UseStopwatch();
                    foreach (var dataFilter in filters)
                    {
                        dataFilter.Execute(context.QueryResult.Data);
                    }

                    context.FilterDataElapsed = Logger.Scope().Stopwatch().Elapsed;
                }

                await ExecuteNextAsync(context);
            }
        }

        internal class RunTest : Step<TestContext>
        {
            public override Task ExecuteAsync(TestContext context)
            {
                throw new NotImplementedException();
            }
        }

        internal class PublishTestResult : Step<TestContext>
        {
            public override Task ExecuteAsync(TestContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}