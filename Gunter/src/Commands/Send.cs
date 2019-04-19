using System.Linq.Custom;
using System.Threading;
using System.Threading.Tasks;
using Gunter.Data;
using JetBrains.Annotations;
using Reusable;
using Reusable.Commander;
using Reusable.Commander.Annotations;
using Reusable.Commander.Services;
using Reusable.Exceptionize;

namespace Gunter.Commands
{
    [Alias("s")]
    internal class Send : ConsoleCommand<ISendParameter, TestContext>
    {
        public Send
        (
            CommandServiceProvider<Send> serviceProvider
        )
            : base(serviceProvider, nameof(Send)) { }

        protected override async Task ExecuteAsync(ICommandLineReader<ISendParameter> parameter, TestContext context, CancellationToken cancellationToken)
        {
            var messenger =
                context
                    .TestBundle
                    .Messengers
                    .SingleOrThrow
                    (
                        m => m.Id.Equals(parameter.GetItem(x => x.Use)),
                        onEmpty: () => DynamicException.Create("MessengerNotFound", $"Could not find messenger '{parameter.GetItem(x => x.Use)}'.")
                    );

            // ReSharper disable once PossibleNullReferenceException - messenger won't be null
            await messenger.SendAsync(context, new SoftString[] { parameter.GetItem(x => x.Report) });
        }
    }

    [UsedImplicitly]
    public interface ISendParameter : ICommandParameter
    {
        [Alias("R")]
        string Report { get; }

        [Alias("U")]
        string Use { get; }
    }
}