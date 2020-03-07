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
using Gunter.Data.SqlClient;
using Gunter.Services;
using Microsoft.Extensions.Caching.Memory;
using Reusable;
using Reusable.Commander;
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

        public bool IsTemplate => Path.GetFileName(Name).StartsWith(Specification.TemplatePrefix);

        public TestBundleType Type =>
            Name is null
                ? TestBundleType.Unknown
                : Path.GetFileName(Name).StartsWith(Specification.TemplatePrefix)
                    ? TestBundleType.Template
                    : TestBundleType.Regular;
    }

    namespace Workflows
    {
        internal class SessionWorkflow : Workflow<SessionContext>
        {
            private SessionWorkflow(IServiceProvider serviceProvider) : base(serviceProvider) { }

            public static SessionWorkflow Create(IServiceProvider serviceProvider) => new SessionWorkflow(serviceProvider)
            {
                new FindProjects(serviceProvider),
                new LoadProjects(serviceProvider),
                new ProcessProjects(serviceProvider)
                {
                    ForEachProject =
                    {
                        new ProcessProject(serviceProvider)
                        {
                            ForEachTest =
                            {
                                new CreateRuntimeProperties(serviceProvider),
                                new GetData(serviceProvider)
                                {
                                    Providers =
                                    {
                                        new GetDataFromTableOrView(default)
                                    }
                                },
                                new FilterData(serviceProvider),
                                new EvaluateData(serviceProvider),
                                new ProcessCommands(serviceProvider)
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

            public List<Specification> TestBundles { get; set; } = new List<Specification>();
        }

        public static class Partial
        {
            public static TResult Read<T, TResult>(this T partial, Func<T, TResult> selector, IEnumerable<Specification> partials) where T : IModel
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
            public LoadProjects(IServiceProvider serviceProvider) : base(serviceProvider) { }

            [Service]
            public ILogger<FindProjects> Logger { get; set; }

            [Service]
            public IResource Resource { get; set; }

            [Service]
            public IPrettyJsonSerializer TestFileSerializer { get; set; }

            public override async Task ExecuteAsync(SessionContext context)
            {
                foreach (var testFile in context.TestFiles.Where(tf => tf.IsTemplate || tf.CanLoad))
                {
                    //using var _ = _logger.BeginScope().WithCorrelationHandle("LoadTestFile").UseStopwatch();

                    //_logger.Log(Abstraction.Layer.IO().Meta(new { TestFileName = fullName }));

                    if (await LoadTestAsync(testFile.Name) is {} testBundle)
                    {
                        context.TestBundles.Add(testBundle.Pipe(x => { x.TestFile = testFile; }));
                    }
                }
            }

            private async Task<Specification?> LoadTestAsync(string fullName)
            {
                try
                {
                    var file = await Resource.ReadTextFileAsync(fullName);
                    var testBundle = TestFileSerializer.Deserialize<Specification>(file, TypeDictionary.From(Specification.SectionTypes)).Pipe(x => x.FullName = fullName);

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
            public List<TestContext> Tests { get; set; } = new List<TestContext>();

            public List<Specification> Templates { get; set; } = new List<Specification>();
        }

        internal class TestContext : IDisposable
        {
            public Specification Specification { get; set; }

            public TestCase TestCase { get; set; }

            public IQuery Query { get; set; }

            public QueryResult? QueryResult { get; set; }

            public TimeSpan GetDataElapsed { get; set; }

            public TimeSpan FilterDataElapsed { get; set; }

            public TimeSpan EvaluateDataElapsed { get; set; }

            public RuntimePropertyProvider Properties { get; set; }

            public TestResult Result { get; set; } = TestResult.Undefined;

            public void Dispose() => QueryResult?.Dispose();
        }

        internal class ProcessProjects : Step<SessionContext>
        {
            public ProcessProjects(IServiceProvider serviceProvider) : base(serviceProvider)
            {
                ForEachProject = new Workflow<ProjectContext>(serviceProvider);
            }

            [Service]
            public ILogger<FindProjects> Logger { get; set; }

            public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount * 2;

            public Workflow<ProjectContext> ForEachProject { get; set; }

            public override async Task ExecuteAsync(SessionContext context)
            {
                var actions = new ActionBlock<ProjectContext>
                (
                    ForEachProject.ExecuteAsync,
                    new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism }
                );

                var projects = context.TestBundles.ToLookup(p => p.TestFile.Type);

                var projectGroups =
                    from testBundle in projects[TestBundleType.Regular]
                    from testCase in testBundle.Tests
                    from query in testCase.Queries(testBundle)
                    group new TestContext
                    {
                        Specification = testBundle,
                        TestCase = testCase,
                        Query = query
                    } by testBundle.Name into projectGroup
                    select projectGroup;

                foreach (var projectGroup in projectGroups)
                {
                    await actions.SendAsync(new ProjectContext
                    {
                        Tests = projectGroup.ToList(),
                        Templates = projects[TestBundleType.Template].ToList()
                    });
                }

                actions.Complete();
                await actions.Completion;
                await ExecuteNextAsync(context);
            }
        }

        internal class ProcessProject : Step<ProjectContext>
        {
            public ProcessProject(IServiceProvider serviceProvider) : base(serviceProvider)
            {
                ForEachTest = new Workflow<TestContext>(serviceProvider);
            }

            public Workflow<TestContext> ForEachTest { get; set; }

            public override async Task ExecuteAsync(ProjectContext context)
            {
                foreach (var testContext in context.Tests)
                {
                    try
                    {
                        await ForEachTest.ExecuteAsync(testContext);
                    }
                    catch (OperationCanceledException)
                    {
                        // log
                    }
                }

                await ExecuteNextAsync(context);
            }
        }

        internal class CreateRuntimeProperties : Step<TestContext>
        {
            public CreateRuntimeProperties(IServiceProvider serviceProvider) : base(serviceProvider) { }

            public override async Task ExecuteAsync(TestContext context)
            {
                var properties = RuntimeProperty.BuiltIn.Enumerate().Concat(context.Specification.Variables.Flatten());
                var objects = new object[] { context.Specification, context.TestCase };
                context.Properties = new RuntimePropertyProvider(properties.ToImmutableList(), objects.ToImmutableList());

                await ExecuteNextAsync(context);
            }
        }

        internal class GetData : Step<TestContext>
        {
            public GetData(IServiceProvider serviceProvider) : base(serviceProvider) { }

            [Service]
            public ILogger<GetData> Logger { get; set; }

            [Service]
            public IMemoryCache Cache { get; set; }

            public List<IGetDataFrom> Providers { get; set; } = new List<IGetDataFrom>();

            public override async Task ExecuteAsync(TestContext context)
            {
                using var scope = Logger.BeginScope().WithCorrelationHandle(nameof(GetData)).UseStopwatch();
                Logger.Log(Abstraction.Layer.Service().Subject(new { QueryId = context.Query.Id }));
                try
                {
                    context.QueryResult = await Cache.GetOrCreateAsync($"{context.Specification.Name}.{context.Query.Id}", async entry =>
                    {
                        foreach (var getDataFrom in Providers)
                        {
                            if (await getDataFrom.ExecuteAsync(context.Query, context.Properties) is {} result)
                            {
                                return result;
                            }
                        }

                        return default;
                    });
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
            public FilterData(IServiceProvider serviceProvider) : base(serviceProvider) { }

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

        internal class EvaluateData : Step<TestContext>
        {
            public EvaluateData(IServiceProvider serviceProvider) : base(serviceProvider) { }

            [Service]
            public ILogger<EvaluateData> Logger { get; set; }

            public override async Task ExecuteAsync(TestContext context)
            {
                using var scope = Logger.BeginScope().WithCorrelationHandle(nameof(EvaluateData)).UseStopwatch();

                if (context.QueryResult.Data.Compute(context.TestCase.Assert, context.TestCase.Filter) is bool success)
                {
                    context.EvaluateDataElapsed = Logger.Scope().Stopwatch().Elapsed;
                    context.Result = success switch
                    {
                        true => TestResult.Passed,
                        false => TestResult.Failed
                    };

                    Logger.Log(Abstraction.Layer.Service().Meta(new { TestResult = context.Result }));
                }
                else
                {
                    throw DynamicException.Create("Assert", $"'{nameof(TestCase.Assert)}' must evaluate to '{nameof(Boolean)}'.");
                }

                await ExecuteNextAsync(context);
            }
        }

        internal class ProcessCommands : Step<TestContext>
        {
            public ProcessCommands(IServiceProvider serviceProvider) : base(serviceProvider) { }

            [Service]
            public ILogger<EvaluateData> Logger { get; set; }

            [Service]
            public ICommandExecutor CommandExecutor { get; set; }

            public override async Task ExecuteAsync(TestContext context)
            {
                foreach (var commandLine in context.TestCase.When.TryGetValue(context.Result, out var then) ? then : Enumerable.Empty<string>())
                {
                    await CommandExecutor.ExecuteAsync(commandLine, context);
                }

                await ExecuteNextAsync(context);
            }
        }
    }
}