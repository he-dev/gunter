using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Custom;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Autofac;
using Autofac.Core;
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
    namespace Workflows
    {
        internal class SessionModule : Module
        {
            protected override void Load(ContainerBuilder builder)
            {
                builder.RegisterGeneric(typeof(InstanceProperty<>));
                builder.RegisterType<Format>().InstancePerDependency();
                builder.RegisterType<Merge>().InstancePerDependency();
                builder.RegisterInstance(new StaticProperty(() => ProgramInfo.FullName));
                builder.RegisterInstance(new StaticProperty(() => ProgramInfo.Version));
                builder.Register(CreateSessionWorkflow);
            }

            private static Workflow<SessionContext> CreateSessionWorkflow(IComponentContext c)
            {
                return c.Resolve<Workflow<SessionContext>>().Pipe(sw =>
                {
                    sw.Add(c.Resolve<FindTestFiles>());
                    sw.Add(c.Resolve<LoadTestFiles>());
                    sw.Add(c.Resolve<ProcessTheories>().Pipe(pt =>
                    {
                        //
                        pt.ForEachTestFile = () => CreateTheoryWorkflow(c);
                    }));
                });
            }

            private static Workflow<TheoryContext> CreateTheoryWorkflow(IComponentContext c)
            {
                return c.Resolve<Workflow<TheoryContext>>().Pipe(tw =>
                {
                    //
                    tw.Add(c.Resolve<ProcessTheory>().Pipe(pt =>
                    {
                        //
                        pt.ForEachTestCase = () => CreateTestWorkflow(c);
                    }));
                });
            }

            private static Workflow<TestContext> CreateTestWorkflow(IComponentContext c)
            {
                return c.Resolve<Workflow<TestContext>>().Pipe(tw =>
                {
                    tw.Add(c.Resolve<GetData>());
                    tw.Add(c.Resolve<FilterData>());
                    tw.Add(c.Resolve<EvaluateData>());
                    tw.Add(c.Resolve<SendMessages>());
                });
            }
        }

        internal class SessionWorkflow : Workflow<SessionContext>
        {
            private SessionWorkflow(IServiceProvider serviceProvider) : base(serviceProvider) { }

            // public static SessionWorkflow Create(IServiceProvider serviceProvider) => new SessionWorkflow(serviceProvider)
            // {
            //     new FindTestFiles(),
            //     new LoadTestFiles(),
            //     new ProcessTheories
            //     {
            //         ForEachTestFile =
            //         {
            //             new ProcessTheory
            //             {
            //                 ForEachTestCase =
            //                 {
            //                     new CreateRuntimeContainer(),
            //                     new GetData()
            //                     {
            //                         Options =
            //                         {
            //                             serviceProvider.GetRequiredService<GetDataFromTableOrView>()
            //                         }
            //                     },
            //                     new FilterData(),
            //                     new EvaluateData(),
            //                     new SendMessages()
            //                 }
            //             }
            //         }
            //     }
            // };
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
            public TestContext(ITheory theory, ITestCase testCase, IQuery query)
            {
                Theory = theory;
                TestCase = testCase;
                Query = query;
            }

            public ITheory Theory { get; }

            public ITestCase TestCase { get; }

            public IQuery Query { get; }

            public string QueryCommand { get; set; }

            public DataTable? Data { get; set; }

            public TimeSpan GetDataElapsed { get; set; }

            public TimeSpan FilterDataElapsed { get; set; }

            public TimeSpan EvaluateDataElapsed { get; set; }

            public TestResult Result { get; set; } = TestResult.Undefined;

            public void Dispose() => Data?.Dispose();
        }

        internal class ProcessTheories : Step<SessionContext>
        {
            public ProcessTheories(ILifetimeScope lifetimeScope)
            {
                LifetimeScope = lifetimeScope;
            }

            private ILifetimeScope LifetimeScope { get; }

            [Service]
            public ILogger<FindTestFiles> Logger { get; set; }

            public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount * 2;

            [Service]
            public Func<Workflow<TheoryContext>> ForEachTestFile { get; set; }

            public override async Task ExecuteAsync(SessionContext context)
            {
                var actions = new ActionBlock<TheoryContext>(ForEachTestFile().ExecuteAsync, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism });

                var testFiles = context.TestFiles.ToLookup(p => p.Type);
                foreach (var theory in testFiles[TestFileType.Regular])
                {
                    using var scope = LifetimeScope.BeginLifetimeScope(builder =>
                    {
                        builder.RegisterInstance(theory).As<ITheory>();
                        builder.RegisterInstance(testFiles[TestFileType.Template]);
                    });
                    await actions.SendAsync(new TheoryContext { Theory = theory, Templates = testFiles[TestFileType.Template] });
                }

                actions.Complete();
                await actions.Completion;
                await ExecuteNextAsync(context);
            }
        }

        internal class ProcessTheory : Step<TheoryContext>
        {
            public ProcessTheory(ILifetimeScope lifetimeScope)
            {
                LifetimeScope = lifetimeScope;
            }

            private ILifetimeScope LifetimeScope { get; }

            public Func<Workflow<TestContext>> ForEachTestCase { get; set; }

            public override async Task ExecuteAsync(TheoryContext context)
            {
                var testCases =
                    from testCase in context.Theory.Tests
                    from queryName in testCase.QueryNames
                    join query in context.Theory.Queries on queryName equals query.Name
                    select (testCase, query);

                foreach (var (testCase, query) in testCases)
                {
                    using var scope = LifetimeScope.BeginLifetimeScope(builder =>
                    {
                        builder.RegisterInstance(new MemoryCache(new MemoryCacheOptions()));
                        builder.RegisterInstance(testCase);
                        builder.RegisterInstance(query).As<IQuery>();
                        builder.Register(c => c.Resolve<InstanceProperty<TestCase>.Factory>()(x => x.Level)).As<IProperty>();
                        builder.Register(c => c.Resolve<InstanceProperty<TestCase>.Factory>()(x => x.Message)).As<IProperty>();
                        builder.Register(c => c.Resolve<InstanceProperty<TestContext>.Factory>()(x => x.GetDataElapsed)).As<IProperty>();
                    });

                    try
                    {
                        await ForEachTestCase().ExecuteAsync(scope.Resolve<TestContext>());
                    }
                    catch (OperationCanceledException)
                    {
                        // log
                    }
                }

                await ExecuteNextAsync(context);
            }
        }

        internal class GetData : Step<TestContext>
        {
            public GetData(ILogger<GetData> logger, IMemoryCache cache)
            {
                Logger = logger;
                Cache = cache;
            }

            public ILogger<GetData> Logger { get; set; }

            public IMemoryCache Cache { get; set; }

            public List<IGetDataFrom> Options { get; set; } = new List<IGetDataFrom>();

            public override async Task ExecuteAsync(TestContext context)
            {
                using var scope = Logger.BeginScope().WithCorrelationHandle(nameof(GetData)).UseStopwatch();
                Logger.Log(Abstraction.Layer.Service().Subject(new { QueryId = context.Query.Name }));
                try
                {
                    (context.QueryCommand, context.Data) = await Cache.GetOrCreateAsync($"{context.Theory.Name}.{context.Query.Name}", async entry =>
                    {
                        if (Options.Single(o => o.QueryType.IsInstanceOfType(context.Query)) is {} getData)
                        {
                            return await getData.ExecuteAsync(context.Query);
                        }

                        return default;
                    });
                    context.GetDataElapsed = Logger.Scope().Stopwatch().Elapsed;
                    Logger.Log(Abstraction.Layer.Database().Counter(new { RowCount = context.Data.Rows.Count, ColumnCount = context.Data.Columns.Count }));
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
            [Service]
            public ILogger<FilterData> Logger { get; set; }

            public override async Task ExecuteAsync(TestContext context)
            {
                if (context.Query.Filters is {} filters)
                {
                    using var scope = Logger.BeginScope().WithCorrelationHandle(nameof(FilterData)).UseStopwatch();
                    foreach (var dataFilter in filters)
                    {
                        dataFilter.Execute(context.Data);
                    }

                    context.FilterDataElapsed = Logger.Scope().Stopwatch().Elapsed;
                }

                await ExecuteNextAsync(context);
            }
        }

        internal class EvaluateData : Step<TestContext>
        {
            [Service]
            public ILogger<EvaluateData> Logger { get; set; }

            public override async Task ExecuteAsync(TestContext context)
            {
                using var scope = Logger.BeginScope().WithCorrelationHandle(nameof(EvaluateData)).UseStopwatch();

                if (context.Data.Compute(context.TestCase.Assert, context.TestCase.Filter) is bool success)
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

        internal class SendMessages : Step<TestContext>
        {
            [Service]
            public ILogger<EvaluateData> Logger { get; set; }

            [Service]
            public ICommandExecutor CommandExecutor { get; set; }

            [Service]
            public IServiceProvider ServiceProvider { get; set; }

            public override async Task ExecuteAsync(TestContext context)
            {
                if (context.TestCase.Messages.TryGetValue(context.Result, out var messages))
                {
                    foreach (var message in messages)
                    {
                        switch (message)
                        {
                            case IEmail email:
                                await ServiceProvider.GetRequiredService<SendEmail>().InvokeAsync(context);
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

    public class Format
    {
        public Format(IEnumerable<IProperty> runtimeProperties)
        {
            RuntimeProperties = runtimeProperties;
        }

        private IEnumerable<IProperty> RuntimeProperties { get; }

        public virtual string Execute(string value)
        {
            return value.Format(name => (string?)RuntimeProperties.FirstOrDefault(p => p.Name.Equals(name))?.GetValue());
        }
    }

    public class Merge
    {
        public Merge(Format format, IEnumerable<Theory> templates)
        {
            Format = format;
            Templates = templates;
        }

        private Format Format { get; }
        
        private IEnumerable<Theory> Templates { get; }

        public virtual TValue Execute<T, TValue>(T instance, Func<T, TValue> getValue)
        {
            var models = Templates.OfType<T>();
            var values = models.Select(getValue).Prepend(getValue(instance));

            foreach (var value in values)
            {
                if (value is {})
                {
                    return value switch
                    {
                        string s => (TValue)(object)Format.Execute(s),
                        _ => value
                    };
                }
            }

            return default;
        }
    }

    public static class MergeHelper
    {
        public static TValue MergeWith<T, TValue>(this T instance, Func<T, TValue> getValue, Merge merge) => merge.Execute(instance, getValue);

        public static IMerge<T, TValue> Merge<T, TValue>(this T instance, Func<T, TValue> getValue) => new Merge<T, TValue>
        {
            Instance = instance,
            GetValue = getValue
        };

    }

    public interface IMerge<T, TValue>
    {
        TValue With(Merge merge);
    }

    public class Merge<T, TValue> : IMerge<T, TValue>
    {
        public T Instance { get; set; }

        public Func<T, TValue> GetValue { get; set; }

        public TValue With(Merge merge) => merge.Execute(Instance, GetValue);
    }

    public static class FormatHelper
    {
        public static string FormatWith(this string value, Format format) => format.Execute(value);
    }
}