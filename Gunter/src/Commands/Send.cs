using System.Linq;
using System.Linq.Custom;
using System.Threading;
using System.Threading.Tasks;
using Gunter.Data;
using JetBrains.Annotations;
using Reusable;
using Reusable.Commander;
using Reusable.Commander.Annotations;
using Reusable.Data.Annotations;
using Reusable.Exceptionize;
using Reusable.OmniLog.Abstractions;

namespace Gunter.Commands
{
    [Tags("s")]
    internal class Send : Command<SendCommandLine, TestContext>
    {
        public Send(ILogger<Send> logger) : base(logger) { }

        protected override async Task ExecuteAsync(SendCommandLine commandLine, TestContext context, CancellationToken cancellationToken)
        {
            var messenger =
                context
                    .TestBundle
                    .Messengers
                    .Where(m => m.Id.Equals(commandLine.Use))
                    .SingleOrThrow
                    (
                        onEmpty: () => DynamicException.Create("MessengerNotFound", $"Could not find messenger '{commandLine.Use}'.")
                    );

            // ReSharper disable once PossibleNullReferenceException - messenger won't be null
            await messenger.SendAsync(context, new SoftString[] { commandLine.Report });
        }
    }

    [UsedImplicitly]
    public class SendCommandLine : CommandLine
    {
        public SendCommandLine(CommandLineDictionary arguments) : base(arguments) { }

        [Tags("R")]
        public string Report => GetArgument(() => Report);

        [Tags("U")]
        public string Use => GetArgument(() => Use);
    }
}