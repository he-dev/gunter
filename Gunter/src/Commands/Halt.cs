using System;
using System.Threading;
using System.Threading.Tasks;
using Gunter.Data;
using Reusable.Commander;
using Reusable.Commander.Annotations;
using Reusable.Data.Annotations;

namespace Gunter.Commands
{
    [Tags("h")]
    internal class Halt : Command<ICommandArgumentGroup, TestContext>
    {
        public Halt
        (
            CommandServiceProvider<Halt> serviceProvider
        )
            : base(serviceProvider) { }

        protected override Task ExecuteAsync(ICommandLineReader<ICommandArgumentGroup> parameter, TestContext context, CancellationToken cancellationToken)
        {
            throw new OperationCanceledException();
        }
    }
}