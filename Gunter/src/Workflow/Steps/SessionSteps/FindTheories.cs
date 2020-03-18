using System.Linq;
using System.Threading.Tasks;
using Gunter.Workflow.Data;
using Reusable;
using Reusable.Extensions;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Data;
using Reusable.IO;

namespace Gunter.Workflow.Steps.SessionSteps
{
    internal class FindTheories : Step<SessionContext>
    {
        public FindTheories(IDirectoryTree directoryTree)
        {
            DirectoryTree = directoryTree;
        }

        private IDirectoryTree DirectoryTree { get; set; }

        protected override Task<Flow> ExecuteBody(SessionContext context)
        {
            context.TheoryNames =
                DirectoryTree
                    .Walk(context.TheoryDirectoryName, DirectoryTreePredicates.MaxDepth(1), PhysicalDirectoryTree.IgnoreExceptions)
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

            return Flow.Continue.ToTask();
        }
    }
}