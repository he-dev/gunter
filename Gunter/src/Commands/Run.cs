using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gunter.Data.Workflows;
using Gunter.Services;
using Gunter.Workflow.Data;
using JetBrains.Annotations;
using Reusable.Commander;
using Reusable.Data.Annotations;
using Reusable.Flowingo.Steps;
using Reusable.OmniLog.Abstractions;
using Reusable.Translucent;

namespace Gunter.Commands
{
    [Tags("b")]
    internal class Run : Command<Run.Parameter>
    {
        private readonly IResource _resource;
        private readonly Workflow<SessionContext> _sessionWorkflow;

        public Run
        (
            ILogger<Run> logger,
            IResource resource,
            Workflow<SessionContext> sessionWorkflow
        )
        {
            _resource = resource;
            _sessionWorkflow = sessionWorkflow;
        }

        protected override async Task ExecuteAsync(Parameter parameter, CancellationToken cancellationToken)
        {
            var currentDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var defaultPath = Path.Combine(currentDirectory, _resource.ReadSetting(ProgramConfig.DefaultTestsDirectoryName));

            //var bundles = await _testLoader.LoadTestsAsync(parameter.Path ?? defaultPath, parameter.Files).ToListAsync(cancellationToken);
            var testFilter = new TheoryFilter
            {
                //DirectoryNamePatterns = parameter.Path ?? defaultPath,
                //Files = commandLine.Files,
                //Tags = parameter.Tests,
                Tags = parameter.Tags
            };
            //var compositions = _testComposer.ComposeTests(bundles, testFilter);
            //await _testRunner.RunAsync(compositions);

            await _sessionWorkflow.ExecuteAsync(new SessionContext
            {
                TheoryDirectoryName = defaultPath,
                TheoryFilter = testFilter
            });
        }

        [UsedImplicitly]
        public class Parameter : CommandParameter
        {
            public string Path { get; set; }
            public List<string> Files { get; set; } = new List<string>();
            public List<string> Tests { get; set; } = new List<string>();
            public List<string> Tags { get; set; } = new List<string>();
        }
    }
}