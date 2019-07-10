using System;
using System.Threading;
using System.Threading.Tasks;
using Gunter.Data;
using Reusable.Commander;
using Reusable.Commander.Annotations;
using Reusable.Data.Annotations;
using Reusable.OmniLog.Abstractions;

namespace Gunter.Commands
{
    [Tags("h")]
    internal class Halt : Command<CommandLine, TestContext>
    {
        public Halt(ILogger<Halt> logger) : base(logger) { }

        protected override Task ExecuteAsync(CommandLine commandLine, TestContext context, CancellationToken cancellationToken)
        {
            throw new OperationCanceledException();
        }
    }
}