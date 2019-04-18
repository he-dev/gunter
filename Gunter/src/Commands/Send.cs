using System.Linq.Custom;
using System.Threading;
using System.Threading.Tasks;
using Gunter.Data;
using JetBrains.Annotations;
using Reusable;
using Reusable.Commander;
using Reusable.Commander.Annotations;
using Reusable.Exceptionize;

namespace Gunter.Commands
{
    [Alias("s")]
    internal class Send : ConsoleCommand<SendBag, TestContext>
    {
        public Send
        (
            CommandServiceProvider<Send> serviceProvider
        )
            : base(serviceProvider, nameof(SendBag)) { }

        protected override async Task ExecuteAsync(SendBag parameter, TestContext context, CancellationToken cancellationToken)
        {
            var messenger =
                context
                    .TestBundle
                    .Messengers
                    .SingleOrThrow
                    (
                        m => m.Id.Equals(parameter.Use),
                        onEmpty: () => DynamicException.Create("MessengerNotFound", $"Could not find messenger '{parameter.Use}'.")
                    );

            // ReSharper disable once PossibleNullReferenceException - messenger won't be null
            await messenger.SendAsync(context, new SoftString[] { parameter.Report });
        }
    }

    [UsedImplicitly]
    public class SendBag : SimpleBag
    {
        [Alias("R")]
        public string Report { get; set; }

        [Alias("U")]
        public string Use { get; set; }
    }
}