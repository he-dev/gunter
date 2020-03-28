using System;
using System.Threading.Tasks;
using Gunter.Services;
using Gunter.Workflow.Data;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Data;
using Reusable.OmniLog;
using Reusable.OmniLog.Nodes;
using Reusable.Translucent;

namespace Gunter.Workflow.Steps.SessionSteps
{
    internal class LoadTheories : Step<SessionContext>
    {
        public LoadTheories
        (
            IResource resource,
            DeserializeTheory deserializeTheory
        )
        {
            Resource = resource;
            DeserializeTheory = deserializeTheory;
        }

        private IResource Resource { get; set; }

        private DeserializeTheory DeserializeTheory { get; }

        protected override async Task<Flow> ExecuteBody(SessionContext context)
        {
            foreach (var theoryFileName in context.TheoryNames)
            {
                using var scope = Logger?.BeginScope("LoadTheory", new { theoryFileName });
                try
                {
                    var prettyJson = await Resource.ReadTextFileAsync(theoryFileName);
                    if (DeserializeTheory.Invoke(theoryFileName, prettyJson) is {} theory)
                    {
                        context.Theories.Add(theory);
                    }
                }
                catch (Exception inner)
                {
                    Logger?.Scope().Exceptions.Push(inner);
                }
            }

            return Flow.Continue;
        }
    }
}