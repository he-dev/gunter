using System.Linq;
using System.Threading.Tasks;
using Gunter.Data.Configuration;
using Gunter.Workflow.Data;
using Reusable.Extensions;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Data;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter.Workflow.Steps.TheorySteps
{
    internal class IgnoreTheoryWithDuplicateModelNames : Step<TheoryContext>
    {
        public IgnoreTheoryWithDuplicateModelNames(Theory theory)
        {
            Theory = theory;
        }

        private Theory Theory { get; }

        protected override Task<Flow> ExecuteBody(TheoryContext context)
        {
            var duplicateModelNames =
                from model in Theory
                group model by model.Name into g
                where g.Count() > 1
                select g.First().Name;

            if (duplicateModelNames.ToList() is var x && x.Any())
            {
                Logger.Log(Abstraction.Layer.IO().Flow().Decision("Ignore theory.").Because("It contains duplicate model names.").Warning());
                Logger.Log(Abstraction.Layer.IO().Meta(new { theoryName = Theory.Name, duplicateModelNames = x }, "DuplicateModelNames").Warning());
                return Flow.Break.ToTask();
            }
            else
            {
                return Flow.Continue.ToTask();
            }
        }
    }
}