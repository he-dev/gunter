using System.Collections.Generic;

namespace Gunter.Data
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

        public class TheoryFilter
        {
            public List<string> DirectoryNamePatterns { get; set; } = new List<string> { ".+" };
            public List<string> FileNamePatterns { get; set; } = new List<string> { ".+" };
            public List<string> TestNamePatterns { get; set; } = new List<string> { ".+" };
            public List<string> Tags { get; set; } = new List<string>();
        }
    }
}