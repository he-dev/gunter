using System;
using System.Collections;
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
using Gunter.Services.Channels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Reusable;
using Reusable.Collections.Generic;
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
using Reusable.Utilities.JsonNet.Converters;
using IMessage = Gunter.Data.IMessage;

namespace Gunter
{
    public class RuntimeContainer : IEnumerable<object>
    {
        private readonly IImmutableList<object> _items;

        public RuntimeContainer() => _items = ImmutableList<object>.Empty;

        public RuntimeContainer(IImmutableList<object> items) => _items = items;

        public static readonly RuntimeContainer Empty = new RuntimeContainer();

        public RuntimeContainer Add(object item) => new RuntimeContainer(_items.Add(item));

        public RuntimeContainer AddRange(IEnumerable<object> items) => new RuntimeContainer(_items.AddRange(items));

        public IEnumerator<object> GetEnumerator() => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_items).GetEnumerator();

        public static implicit operator TryGetValueCallback(RuntimeContainer container) => container.TryGetValue;

        private bool TryGetValue(string name, out object? value)
        {
            return (value = this.OfType<IProperty>().Single(p => p.Name.Equals(name)) switch
            {
                StaticProperty staticProperty => staticProperty.GetValue(null).ToString(),
                InstanceProperty instanceProperty => instanceProperty.GetValue(this.First(item => instanceProperty.SourceType.IsInstanceOfType(item)))
            }) is {};
        }
    }

    namespace Workflows
    {
        internal class SessionWorkflow : Workflow<SessionContext>
        {
            private SessionWorkflow(IServiceProvider serviceProvider) : base(serviceProvider) { }

            public static SessionWorkflow Create(IServiceProvider serviceProvider) => new SessionWorkflow(serviceProvider)
            {
                new FindTestFiles(serviceProvider),
                new LoadTestFiles(serviceProvider),
                new ProcessTheories(serviceProvider)
                {
                    ForEachTestFile =
                    {
                        new ProcessTheory(serviceProvider)
                        {
                            ForEachTestCase =
                            {
                                new CreateRuntimeContainer(serviceProvider),
                                new GetData(serviceProvider)
                                {
                                    Options =
                                    {
                                        new GetDataFromTableOrView(default)
                                    }
                                },
                                new FilterData(serviceProvider),
                                new EvaluateData(serviceProvider),
                                new ProcessMessages(serviceProvider)
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

            public HashSet<string> TestFileNames { get; set; } = new HashSet<string>(SoftString.Comparer);

            public List<Theory> TestFiles { get; set; } = new List<Theory>();
        }

        public class TestFilter
        {
            public List<string> DirectoryNamePatterns { get; set; } = new List<string> { ".+" };
            public List<string> FileNamePatterns { get; set; } = new List<string> { ".+" };
            public List<string> TestNamePatterns { get; set; } = new List<string> { ".+" };
            public List<string> Tags { get; set; } = new List<string>();
        }

        internal class FindTestFiles : Step<SessionContext>
        {
            public FindTestFiles(IServiceProvider serviceProvider) : base(serviceProvider) { }

            [Service]
            public ILogger<FindTestFiles> Logger { get; set; }

            [Service]
            public IDirectoryTree DirectoryTree { get; set; }

            public override async Task ExecuteAsync(SessionContext context)
            {
                context.TestFileNames =
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
                        .ToHashSet(SoftString.Comparer);

                await ExecuteNextAsync(context);
            }
        }

        internal class DeserializeTestFile
        {
            public delegate DeserializeTestFile Factory(string fileName);

            public DeserializeTestFile(IPrettyJson prettyJson, IContractResolver contractResolver, string fileName)
            {
                PrettyJson = prettyJson;
                JsonSerializer = new JsonSerializer
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    ObjectCreationHandling = ObjectCreationHandling.Replace,
                    ContractResolver = contractResolver,
                    Converters =
                    {
                        new StringEnumConverter(),
                        new SoftStringConverter(),
                        new TestFileConverter
                        {
                            FileName = fileName
                        }
                    }
                };
            }

            private IPrettyJson PrettyJson { get; }

            private JsonSerializer JsonSerializer { get; }

            public Theory Invoke(string prettyJson)
            {
                var normalJson = PrettyJson.Read(prettyJson, TypeDictionary.From(Theory.SectionTypes));
                return normalJson.ToObject<Theory>(JsonSerializer);
            }

            private class TestFileConverter : CustomCreationConverter<Theory>
            {
                public string FileName { get; set; }

                public override Theory Create(Type objectType)
                {
                    return new Theory
                    {
                        FullName = FileName
                    };
                }
            }
        }

        internal class LoadTestFiles : Step<SessionContext>
        {
            public LoadTestFiles(IServiceProvider serviceProvider) : base(serviceProvider) { }

            [Service]
            public ILogger<FindTestFiles> Logger { get; set; }

            [Service]
            public IResource Resource { get; set; }

            [Service]
            public IPrettyJson PrettyJson { get; set; }

            [Service]
            public DeserializeTestFile.Factory TestFileSerializerFactory { get; set; }

            public override async Task ExecuteAsync(SessionContext context)
            {
                foreach (var testFileName in context.TestFileNames)
                {
                    //using var _ = _logger.BeginScope().WithCorrelationHandle("LoadTestFile").UseStopwatch();

                    //_logger.Log(Abstraction.Layer.IO().Meta(new { TestFileName = fullName }));

                    if (await LoadTestFileAsync(testFileName) is {} testFile)
                    {
                        context.TestFiles.Add(testFile);
                    }
                }
            }

            private async Task<Theory?> LoadTestFileAsync(string name)
            {
                try
                {
                    var prettyJson = await Resource.ReadTextFileAsync(name);
                    var testFileSerializer = TestFileSerializerFactory(name);
                    var testFile = testFileSerializer.Invoke(prettyJson);

                    if (testFile.Enabled)
                    {
                        var duplicateIds =
                            from model in testFile
                            group model by model.Name into g
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
                            return testFile;
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

        internal class TheoryContext
        {
            public ITheory Theory { get; set; }

            public IEnumerable<ITheory> Templates { get; set; }
        }

        public class TestContext : IDisposable
        {
            public ITheory Theory { get; set; }

            public ITestCase TestCase { get; set; }

            public IQuery Query { get; set; }

            public GetDataResult? GetDataResult { get; set; }

            public TimeSpan GetDataElapsed { get; set; }

            public TimeSpan FilterDataElapsed { get; set; }

            public TimeSpan EvaluateDataElapsed { get; set; }

            public RuntimeContainer Container { get; set; }

            public TestResult Result { get; set; } = TestResult.Undefined;

            public void Dispose() => GetDataResult?.Dispose();
        }

        internal class ProcessTheories : Step<SessionContext>
        {
            public ProcessTheories(IServiceProvider serviceProvider) : base(serviceProvider)
            {
                ForEachTestFile = new Workflow<TheoryContext>(serviceProvider);
            }

            [Service]
            public ILogger<FindTestFiles> Logger { get; set; }

            public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount * 2;

            public Workflow<TheoryContext> ForEachTestFile { get; set; }

            public override async Task ExecuteAsync(SessionContext context)
            {
                var actions = new ActionBlock<TheoryContext>(ForEachTestFile.ExecuteAsync, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism });

                var testFiles = context.TestFiles.ToLookup(p => p.Type);
                foreach (var theory in testFiles[TestFileType.Regular])
                {
                    await actions.SendAsync(new TheoryContext { Theory = theory, Templates = testFiles[TestFileType.Template] });
                }

                actions.Complete();
                await actions.Completion;
                await ExecuteNextAsync(context);
            }
        }

        internal class ProcessTheory : Step<TheoryContext>
        {
            public ProcessTheory(IServiceProvider serviceProvider) : base(serviceProvider)
            {
                ForEachTestCase = new Workflow<TestContext>(serviceProvider);
            }

            public Workflow<TestContext> ForEachTestCase { get; set; }

            public override async Task ExecuteAsync(TheoryContext context)
            {
                var testCases =
                    from testCase in context.Theory.Tests
                    from queryName in testCase.QueryNames
                    join query in context.Theory.Queries on queryName equals query.Name
                    select (testCase, query);

                foreach (var (testCase, query) in testCases)
                {
                    try
                    {
                        await ForEachTestCase.ExecuteAsync(new TestContext
                        {
                            Theory = context.Theory,
                            TestCase = testCase,
                            Query = query
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        // log
                    }
                }

                await ExecuteNextAsync(context);
            }
        }

        internal class CreateRuntimeContainer : Step<TestContext>
        {
            public CreateRuntimeContainer(IServiceProvider serviceProvider) : base(serviceProvider) { }

            public override async Task ExecuteAsync(TestContext context)
            {
                context.Container =
                    RuntimeContainer
                        .Empty
                        .AddRange(RuntimeProperty.BuiltIn.Enumerate())
                        .AddRange(context.Theory.Properties.Flatten())
                        .Add(context.Theory)
                        .Add(context.TestCase);

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

            public List<IGetDataFrom> Options { get; set; } = new List<IGetDataFrom>();

            public override async Task ExecuteAsync(TestContext context)
            {
                using var scope = Logger.BeginScope().WithCorrelationHandle(nameof(GetData)).UseStopwatch();
                Logger.Log(Abstraction.Layer.Service().Subject(new { QueryId = context.Query.Name }));
                try
                {
                    context.GetDataResult = await Cache.GetOrCreateAsync($"{context.Theory.Name}.{context.Query.Name}", async entry =>
                    {
                        if (Options.Single(o => o.QueryType.IsInstanceOfType(context.Query)) is {} getData)
                        {
                            return await getData.ExecuteAsync(context.Query, context.Container);
                        }

                        return default;
                    });
                    context.GetDataElapsed = Logger.Scope().Stopwatch().Elapsed;
                    Logger.Log(Abstraction.Layer.Database().Counter(new { RowCount = context.GetDataResult.Data.Rows.Count, ColumnCount = context.GetDataResult.Data.Columns.Count }));
                    await ExecuteNextAsync(context);
                }
                catch (Exception inner)
                {
                    throw DynamicException.Create(GetType().ToPrettyString(), $"Error getting or processing data for query '{context.Query.Name}'.", inner);
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
                        dataFilter.Execute(context.GetDataResult.Data);
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

                if (context.GetDataResult.Data.Compute(context.TestCase.Assert, context.TestCase.Filter) is bool success)
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

        internal class ProcessMessages : Step<TestContext>
        {
            public ProcessMessages(IServiceProvider serviceProvider) : base(serviceProvider) { }

            [Service]
            public ILogger<EvaluateData> Logger { get; set; }

            [Service]
            public ICommandExecutor CommandExecutor { get; set; }

            public List<IMessage> Messages { get; set; } = new List<IMessage>();

            public override async Task ExecuteAsync(TestContext context)
            {
                if (context.TestCase.Messages.TryGetValue(context.Result, out var messages))
                {
                    foreach (var message in messages)
                    {
                        switch (message)
                        {
                            case IEmail email:
                                await ServiceProvider.GetRequiredService<SendEmail>().InvokeAsync(context, email);
                                break;
                        }
                    }
                }

                await ExecuteNextAsync(context);
            }
        }
    }

    public static class Search
    {
        public static TValue Resolve<T, TValue>(this T obj, Func<T, TValue> getValue, Func<TValue, bool> success, Func<T, IEnumerable<TValue>> values)
        {
            return values(obj).Prepend(getValue(obj)).FirstOrDefault(success);
        }
    }
}