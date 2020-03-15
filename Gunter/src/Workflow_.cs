using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.IO;
using System.Linq.Custom;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Gunter.Data;
using Gunter.Data.Configuration;
using Gunter.Queries;
using Gunter.Reporting;
using Gunter.Services;
using Gunter.Services.Abstractions;
using Gunter.Services.Reporting;
using Gunter.Workflow.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reusable;
using Reusable.Collections.Generic;
using Reusable.Commander;
using Reusable.Extensions;
using Reusable.Flowingo.Steps;

namespace Gunter
{
    namespace Workflows
    {
        internal class SessionModule : Module
        {
            protected override void Load(ContainerBuilder builder)
            {
                builder.RegisterGeneric(typeof(InstanceProperty<>));
                builder.RegisterGeneric(typeof(Workflow<>)).InstancePerDependency();
                builder.RegisterType<Format>().InstancePerDependency();
                builder.RegisterType<Merge>().InstancePerDependency();
                builder.RegisterInstance(new StaticProperty(() => ProgramInfo.FullName));
                builder.RegisterInstance(new StaticProperty(() => ProgramInfo.Version));
                builder.RegisterType<GetDataTableOrView>().As<IGetData>();
                builder.RegisterType<DispatchEmail>().As<IDispatchMessage>().InstancePerDependency();
                builder.RegisterType<RenderDataSummary>();
                builder.RegisterType<RenderQueryInfo>();

                builder.Register(c => c.Resolve<Workflow<SessionContext>>().Pipe(sessionWorkflow =>
                {
                    sessionWorkflow.Add(c.Resolve<FindTheoryFiles>());
                    sessionWorkflow.Add(c.Resolve<LoadTheoryFiles>());
                    sessionWorkflow.Add(c.Resolve<ProcessTheories>().Pipe(processTheories =>
                    {
                        processTheories.ForEachTheory = theoryComponents => theoryComponents.Resolve<Workflow<TheoryContext>>().Pipe(theoryWorkflow =>
                        {
                            theoryWorkflow.Add(theoryComponents.Resolve<ProcessTheory>().Pipe(processTheory =>
                            {
                                processTheory.ForEachTestCase = testCaseComponents => testCaseComponents.Resolve<Workflow<TestContext>>().Pipe(testWorkflow =>
                                {
                                    testWorkflow.Add(testCaseComponents.Resolve<GetData>());
                                    testWorkflow.Add(testCaseComponents.Resolve<FilterData>());
                                    testWorkflow.Add(testCaseComponents.Resolve<EvaluateData>());
                                    testWorkflow.Add(testCaseComponents.Resolve<SendMessages>());
                                });
                            }));
                        });
                    }));
                }));
            }
        }

        // internal class SessionWorkflow : Workflow<SessionContext>
        // {
        //     // public static SessionWorkflow Create(IServiceProvider serviceProvider) => new SessionWorkflow(serviceProvider)
        //     // {
        //     //     new FindTheoryFiles(),
        //     //     new LoadTheoryFiles(),
        //     //     new ProcessTheories
        //     //     {
        //     //         ForEachTheory =
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
    }
}