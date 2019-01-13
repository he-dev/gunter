using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    [Alias("e")]
    public class Explicit : ConsoleCommand<ExplicitBag>
    {
        private readonly ITestRunner _testRunner;

        public Explicit(CommandServiceProvider<Explicit> serviceProvider, ITestRunner testRunner) : base(serviceProvider, nameof(Batch))
        {
            _testRunner = testRunner;
        }

        protected override async Task ExecuteAsync(ExplicitBag parameter, CancellationToken cancellationToken)
        {
            await _testRunner.RunTestsAsync
            (
                parameter.Path,
                new[] { new TestFilter(parameter.Test) { Ids = parameter.Ids.Select(SoftString.Create) } },
                null
            );
        }
    }

    public class ExplicitBag : SimpleBag
    {
        [Required]
        public string Path { get; set; }

        [Required]
        public string Test { get; set; }

        [Required]
        public IEnumerable<string> Ids { get; set; }
    }
}
