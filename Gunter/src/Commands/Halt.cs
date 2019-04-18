using System;
using System.Threading;
using System.Threading.Tasks;
using Gunter.Data;
using Reusable.Commander;
using Reusable.Commander.Annotations;

namespace Gunter.Commands
{
    [Alias("h")]
    internal class Halt : ConsoleCommand<SimpleBag, TestContext>
    {
        public Halt
        (
            CommandServiceProvider<Halt> serviceProvider
        )
            : base(serviceProvider, nameof(SimpleBag)) { }

        protected override Task ExecuteAsync(SimpleBag parameter, TestContext context, CancellationToken cancellationToken)
        {
            throw new OperationCanceledException();
        }
    }
}