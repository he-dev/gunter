using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable;
using Reusable.Commander;
using Reusable.Commander.Annotations;

namespace Gunter.Commands
{
    [Alias("b")]
    public class Batch : ConsoleCommand<BatchBag>
    {
        private readonly ITestRunner _testRunner;
        private readonly ProgramInfo _programInfo;

        public Batch
        (
            CommandServiceProvider<Batch> serviceProvider,
            ITestRunner testRunner,
            ProgramInfo programInfo
        )
            : base(serviceProvider, nameof(Batch))
        {
            _testRunner = testRunner;
            _programInfo = programInfo;
        }

        protected override async Task ExecuteAsync(BatchBag parameter, CancellationToken cancellationToken)
        {
            var currentDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var defaultPath = Path.Combine(currentDirectory, _programInfo.DefaultTestsDirectoryName);

            await _testRunner.RunTestsAsync
            (
                string.IsNullOrWhiteSpace(parameter.Path)
                    ? defaultPath :
                    Path.IsPathRooted(parameter.Path)
                        ? parameter.Path
                        : Path.Combine(defaultPath, parameter.Path),
                parameter.Tests.Select(testName => new TestFilter(testName)),
                parameter.Profiles?.Select(SoftString.Create)
            );
        }
    }

    public class BatchBag : SimpleBag
    {
        public string Path { get; set; }

        public IList<string> Tests { get; set; }

        public IList<string> Profiles { get; set; }
    }
}
