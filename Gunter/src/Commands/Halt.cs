using System;
using System.Threading;
using System.Threading.Tasks;
using Gunter.Data;
using Reusable.Commander;
using Reusable.Data.Annotations;
using Reusable.OmniLog.Abstractions;

namespace Gunter.Commands
{
    [Tags("h")]
    internal class Halt : Command<CommandLineBase, TestContext>
    {
        public Halt(ILogger<Halt> logger) : base(logger) { }

        protected override Task ExecuteAsync(CommandLineBase commandLine, TestContext context, CancellationToken cancellationToken)
        {
            throw new OperationCanceledException();
        }
    }
}