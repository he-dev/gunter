using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Gunter.Data;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable.Commander;
using Reusable.Data.Annotations;
using Reusable.OmniLog.Abstractions;
using Reusable.Translucent;

namespace Gunter.Commands
{
    [Tags("b")]
    internal class Run : Command<Run.Parameter>
    {
        private readonly ITestLoader _testLoader;
        private readonly ITestComposer _testComposer;
        private readonly ITestRunner _testRunner;
        private readonly IResourceRepository _resources;

        public Run
        (
            ILogger<Run> logger,
            ITestLoader testLoader,
            ITestComposer testComposer,
            ITestRunner testRunner,
            IResourceRepository resources
        )
            : base(logger)
        {
            _testLoader = testLoader;
            _testComposer = testComposer;
            _testRunner = testRunner;
            _resources = resources;
        }

        protected override async Task ExecuteAsync(Parameter parameter, CancellationToken cancellationToken)
        {
            var currentDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var defaultPath = Path.Combine(currentDirectory, _resources.ReadSetting(ProgramConfig.DefaultTestsDirectoryName));

            var bundles = await _testLoader.LoadTestsAsync(parameter.Path ?? defaultPath, parameter.Files);
            var testFilter = new TestFilter
            {
                Path = parameter.Path ?? defaultPath,
                //Files = commandLine.Files,
                Tests = parameter.Tests,
                Tags = parameter.Tags
            };
            var compositions = _testComposer.ComposeTests(bundles, testFilter);
            await _testRunner.RunAsync(compositions);
        }

        [UsedImplicitly]
        public class Parameter : CommandParameter
        {
            public string Path { get; set; }

            public IList<string> Files { get; set; }

            public IList<string> Tests { get; set; }

            public IList<string> Tags { get; set; }
        }
    }
}