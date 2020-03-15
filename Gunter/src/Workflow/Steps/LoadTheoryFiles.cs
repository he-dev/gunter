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
}