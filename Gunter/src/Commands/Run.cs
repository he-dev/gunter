using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gunter.Data;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable;
using Reusable.Commander;
using Reusable.Commander.Annotations;

namespace Gunter.Commands
{
    [Alias("b")]
    internal class Run : ConsoleCommand<RunBag>
    {
        private readonly ITestLoader _testLoader;
        private readonly ITestComposer _testComposer;
        private readonly ITestRunner _testRunner;
        private readonly ProgramInfo _programInfo;

        public Run
        (
            CommandServiceProvider<Run> serviceProvider,
            ITestLoader testLoader,
            ITestComposer testComposer,
            ITestRunner testRunner,
            ProgramInfo programInfo
        )
            : base(serviceProvider, nameof(RunBag))
        {
            _testLoader = testLoader;
            _testComposer = testComposer;
            _testRunner = testRunner;
            _programInfo = programInfo;
        }

        protected override async Task ExecuteAsync(RunBag parameter, CancellationToken cancellationToken)
        {
            var currentDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var defaultPath = Path.Combine(currentDirectory, _programInfo.DefaultTestsDirectoryName);

            parameter.Path = parameter.Path ?? defaultPath;
            
            var bundles = await _testLoader.LoadTestsAsync(parameter.Path);
            var compositions = _testComposer.ComposeTests(bundles, parameter);
            await _testRunner.RunAsync(compositions);
        }
    }

    [UsedImplicitly]
    public class RunBag : SimpleBag, ITestFilter
    {
        public string Path { get; set; }

        public IList<string> Files { get; set; }

        public IList<string> Tests { get; set; }

        public IList<string> Tags { get; set; }
    }
}