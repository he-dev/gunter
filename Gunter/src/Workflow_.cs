using System;
using System.Collections.Generic;
using System.Data;
using Gunter.Data;
using Gunter.Data.Configuration;
using Reusable;

namespace Gunter
{
    namespace Workflows
    {
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

            public List<Theory> Theories { get; set; } = new List<Theory>();
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
            public TestContext(Theory theory, TestCase testCase, IQuery query)
            {
                Theory = theory;
                TestCase = testCase;
                Query = query;
            }

            public Theory Theory { get; }

            public TestCase TestCase { get; }

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