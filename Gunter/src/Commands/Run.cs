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
    internal class Run : Command<Run.CommandLine, object>
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

        protected override async Task ExecuteAsync(CommandLine commandLine, object context, CancellationToken cancellationToken)
        {
            var currentDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var defaultPath = Path.Combine(currentDirectory, _resources.ReadSetting(ProgramConfig.DefaultTestsDirectoryName));

            var bundles = await _testLoader.LoadTestsAsync(commandLine.Path ?? defaultPath, commandLine.Files);
            var testFilter = new TestFilter
            {
                Path = commandLine.Path ?? defaultPath,
                //Files = commandLine.Files,
                Tests = commandLine.Tests,
                Tags = commandLine.Tags
            };
            var compositions = _testComposer.ComposeTests(bundles, testFilter);
            await _testRunner.RunAsync(compositions);
        }

        [UsedImplicitly]
        public class CommandLine : CommandLineBase
        {
            public CommandLine(CommandLineDictionary arguments) : base(arguments) { }

            public string Path => GetArgument(() => Path);

            public IList<string> Files => GetArgument(() => Files);

            public IList<string> Tests => GetArgument(() => Tests);

            public IList<string> Tags => GetArgument(() => Tags);
        }
    }
}