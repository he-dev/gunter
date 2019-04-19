using System;
using System.Threading;
using System.Threading.Tasks;
using Gunter.Data;
using Reusable.Commander;
using Reusable.Commander.Annotations;
using Reusable.Commander.Services;

namespace Gunter.Commands
{
    [Alias("h")]
    internal class Halt : ConsoleCommand<ICommandParameter, TestContext>
    {
        public Halt
        (
            CommandServiceProvider<Halt> serviceProvider
        )
            : base(serviceProvider, nameof(SimpleBag)) { }

        protected override Task ExecuteAsync(ICommandLineReader<ICommandParameter> parameter, TestContext context, CancellationToken cancellationToken)
        {
            throw new OperationCanceledException();
        }
    }
}