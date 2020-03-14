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
using Autofac.Builder;
using Autofac.Core;
using Gunter.Data;
using Gunter.Data.Configuration;
using Gunter.Data.Reporting;
using Gunter.Reporting;
using Gunter.Services;
using Gunter.Services.Channels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
                builder.RegisterType<GetDataFromTableOrView>().As<IGetDataFrom>();
                builder.RegisterType<DispatchEmail>().As<IDispatchMessage>().InstancePerDependency();
                builder.RegisterType<RenderDataInfo>();
                builder.RegisterType<RenderQueryInfo>();
                builder.Register(c => new Workflow<SessionContext>
                {
                    c.Resolve<FindTheoryFiles>(),
                    c.Resolve<LoadTheoryFiles>(),
                    c.Resolve<ProcessTheories>()
                }).InstancePerDependency();

                builder.Register(c => new Workflow<TheoryContext>
                {
                    c.Resolve<ProcessTheory>()
                }).InstancePerDependency();

                builder.Register(c => new Workflow<TestContext>
                {
                    c.Resolve<GetData>(),
                    c.Resolve<FilterData>(),
                    c.Resolve<EvaluateData>(),
                    c.Resolve<SendMessages>(),
                }).InstancePerDependency();
            }
        }

        // internal class SessionWorkflow : Workflow<SessionContext>
        // {
        //     // public static SessionWorkflow Create(IServiceProvider serviceProvider) => new SessionWorkflow(serviceProvider)
        //     // {
        //     //     new FindTestFiles(),
        //     //     new LoadTestFiles(),
        //     //     new ProcessTheories
        //     //     {
        //     //         ForEachTestFile =
        //     //         {
        //     //             new ProcessTheory
        //     //             {
        //     //                 ForEachTestCase =
        //     //                 {
        //     //                     new CreateRuntimeContainer(),
        //     //                     new GetData()
        //     //                     {
        //     //                         Options =
        //     //                         {
        //     //                             serviceProvider.GetRequiredService<GetDataFromTableOrView>()
        //     //                         }
        //     //                     },
        //     //                     new FilterData(),
        //     //                     new EvaluateData(),
        //     //                     new SendMessages()
        //     //                 }
        //     //             }
        //     //         }
        //     //     }
        //     // };
        // }

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

        internal class FindTheoryFiles : Step<SessionContext>
        {
            [Service]
            public ILogger<FindTheoryFiles> Logger { get; set; }

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

        internal class LoadTheoryFiles : Step<SessionContext>
        {
            [Service]
            public ILogger<FindTheoryFiles> Logger { get; set; }

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
            //public ITheory Theory { get; set; }

            //public IEnumerable<ITheory> Templates { get; set; }
        }

        public class TestContext : IDisposable
        {
            public TestContext(Theory theory, ITestCase testCase, IQuery query)
            {
                Theory = theory;
                TestCase = testCase;
                Query = query;
            }

            public Theory Theory { get; }

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
            public ProcessTheories(ILogger<ProcessTheories> logger, ILifetimeScope lifetimeScope)
            {
                Logger = logger;
                LifetimeScope = lifetimeScope;
            }

            private ILifetimeScope LifetimeScope { get; }

            private ILogger<ProcessTheories> Logger { get; }

            //public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount * 2;

            public override async Task ExecuteAsync(SessionContext context)
            {
                var theories = context.TestFiles.ToLookup(p => p.Type);
                var theoryWorkflowTasks = theories[TheoryType.Regular].Select(theory => ProcessTheory(theory, theories[TheoryType.Template]));
                await Task.WhenAll(theoryWorkflowTasks);
                await ExecuteNextAsync(context);
            }

            private async Task ProcessTheory(Theory theory, IEnumerable<Theory> templates)
            {
                using var scope = LifetimeScope.BeginLifetimeScope(builder =>
                {
                    builder.RegisterInstance(theory);
                    builder.RegisterInstance(templates);
                });

                await scope.Resolve<Workflow<TheoryContext>>().ExecuteAsync(default);
            }
        }

        internal class ProcessTheory : Step<TheoryContext>
        {
            public ProcessTheory(ILifetimeScope lifetimeScope, Theory theory)
            {
                LifetimeScope = lifetimeScope;
                Theory = theory;
            }

            private ILifetimeScope LifetimeScope { get; }

            private Theory Theory { get; }

            public override async Task ExecuteAsync(TheoryContext context)
            {
                var testCases =
                    from testCase in Theory.Tests
                    from queryName in testCase.QueryNames
                    join query in Theory.Queries on queryName equals query.Name
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
                        await scope.Resolve<Workflow<TestContext>>().ExecuteAsync(scope.Resolve<TestContext>());
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
            public GetData(ILogger<GetData> logger, IMemoryCache cache, IEnumerable<IGetDataFrom> getDataFromCommands)
            {
                Logger = logger;
                Cache = cache;
                GetDataFromCommands = getDataFromCommands;
            }

            private ILogger<GetData> Logger { get; }

            private IMemoryCache Cache { get; }

            private IEnumerable<IGetDataFrom> GetDataFromCommands { get; }

            public override async Task ExecuteAsync(TestContext context)
            {
                using var scope = Logger.BeginScope().WithCorrelationHandle(nameof(GetData)).UseStopwatch();
                Logger.Log(Abstraction.Layer.Service().Subject(new { QueryId = context.Query.Name }));
                try
                {
                    (context.QueryCommand, context.Data) = await Cache.GetOrCreateAsync($"{context.Theory.Name}.{context.Query.Name}", async entry =>
                    {
                        if (GetDataFromCommands.Single(o => o.QueryType.IsInstanceOfType(context.Query)) is {} getData)
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
            public SendMessages(ILogger<SendMessages> logger, IComponentContext componentContext)
            {
                Logger = logger;
                ComponentContext = componentContext;
            }

            private ILogger<SendMessages> Logger { get; set; }

            private IComponentContext ComponentContext { get; }

            public override async Task ExecuteAsync(TestContext context)
            {
                if (context.TestCase.Messages.TryGetValue(context.Result, out var messages))
                {
                    foreach (var message in messages)
                    {
                        switch (message)
                        {
                            case Email email:
                                await ComponentContext.Resolve<DispatchEmail>().InvokeAsync(email);
                                break;
                        }
                    }
                }

                await ExecuteNextAsync(context);
            }
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
        public static IMerge<TValue> Merge<T, TValue>(this T instance, Func<T, TValue> getValue) => new Merge<T, TValue>
        {
            Instance = instance,
            GetValue = getValue
        };
    }

    public interface IMerge<out TValue>
    {
        TValue With(Merge merge);
    }

    public class Merge<T, TValue> : IMerge<TValue>
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