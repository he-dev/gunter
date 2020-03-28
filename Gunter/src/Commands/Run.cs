using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly ILogger<Run> _logger;
        private readonly IResource _resource;
        private readonly Workflow<SessionContext> _sessionWorkflow;

        public Run
        (
            ILogger<Run> logger,
            IResource resource,
            Workflow<SessionContext> sessionWorkflow
        )
        {
            _logger = logger;
            _resource = resource;
            _sessionWorkflow = sessionWorkflow;
        }

        protected override async Task ExecuteAsync(Parameter parameter, CancellationToken cancellationToken)
        {
            var currentDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
            var defaultPath = Path.Combine(currentDirectory, _resource.ReadSetting(ProgramConfig.DefaultTestsDirectoryName));

            await _sessionWorkflow.ExecuteAsync(new SessionContext
            {
                TheoryDirectoryName = defaultPath,
                TestFilter =
                {
                    FileNamePatterns = parameter.Files,
                    TestNamePatterns = parameter.Tests,
                    Tags = parameter.Tags
                }
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