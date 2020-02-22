using System;
using System.Threading;
using System.Threading.Tasks;
using Reusable.Commander;
using Reusable.Data.Annotations;
using Reusable.OmniLog.Abstractions;

namespace Gunter.Commands
{
    [Tags("h")]
    internal class Halt : Command<CommandParameter>
    {
        public Halt(ILogger<Halt> logger) : base(logger) { }

        protected override Task ExecuteAsync(CommandParameter parameter, CancellationToken cancellationToken)
        {
            throw new OperationCanceledException();
        }
    }
}