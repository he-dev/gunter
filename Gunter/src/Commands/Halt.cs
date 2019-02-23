using System;
using System.Threading;
using System.Threading.Tasks;
using Gunter.Data;
using Reusable.Commander;
using Reusable.Commander.Annotations;

namespace Gunter.Commands
{
    [Alias("h")]
    internal class Halt : ConsoleCommand<RunBag, TestContext>
    {
        private readonly ProgramInfo _programInfo;

        public Halt
        (
            CommandServiceProvider<Run> serviceProvider
        )
            : base(serviceProvider, nameof(RunBag)) { }

        protected override Task ExecuteAsync(RunBag parameter, TestContext context, CancellationToken cancellationToken)
        {
            throw new OperationCanceledException();
        }
    }
}