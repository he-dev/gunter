using System;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Data.Configuration;
using Gunter.Services;
using Gunter.Workflows;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Annotations;
using Reusable.OmniLog.Abstractions;
using Reusable.Translucent;
using Reusable.Utilities.JsonNet;

namespace Gunter.Workflow.Steps
{
    internal class LoadTheories : Step<SessionContext>
    {
        public LoadTheories
        (
            ILogger<LoadTheories> logger,
            IResource resource,
            DeserializeTheory deserializeTheory
        )
        {
            Logger = logger;
            Resource = resource;
            DeserializeTheory = deserializeTheory;
        }

        private ILogger<LoadTheories> Logger { get; set; }

        private IResource Resource { get; set; }
        
        private DeserializeTheory DeserializeTheory { get; }

        public override async Task ExecuteAsync(SessionContext context)
        {
            foreach (var testFileName in context.TestFileNames)
            {
                //using var _ = _logger.BeginScope().WithCorrelationHandle("LoadTestFile").UseStopwatch();

                //_logger.Log(Abstraction.Layer.IO().Meta(new { TestFileName = fullName }));

                if (await DeserializeTheoryAsync(testFileName) is {} theory)
                {
                    context.Theories.Add(theory);
                }
            }

            await ExecuteNextAsync(context);
        }

        private async Task<Theory?> DeserializeTheoryAsync(string name)
        {
            try
            {
                var prettyJson = await Resource.ReadTextFileAsync(name);
                var theory = DeserializeTheory.Invoke(name, prettyJson);

                if (theory.Enabled)
                {
                    var duplicateIds =
                        from model in theory
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
                        return theory;
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
}