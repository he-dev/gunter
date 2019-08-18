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
    internal class Send : Command<Send.CommandLine, TestContext>
    {
        public Send(ILogger<Send> logger) : base(logger) { }

        protected override async Task ExecuteAsync(CommandLine commandLine, TestContext context, CancellationToken cancellationToken)
        {
            var messenger =
                context
                    .TestBundle
                    .Channels
                    .Where(m => m.Id.Equals(commandLine.Channel))
                    .SingleOrThrow
                    (
                        onEmpty: () => DynamicException.Create("MessengerNotFound", $"Could not find messenger '{commandLine.Channel}'.")
                    );

            // ReSharper disable once PossibleNullReferenceException - messenger won't be null
            await messenger.SendAsync(context, new SoftString[] { commandLine.Report });
        }

        [UsedImplicitly]
        public class CommandLine : CommandLineBase
        {
            public CommandLine(CommandLineDictionary arguments) : base(arguments) { }

            [Tags("R")]
            public string Report => GetArgument(() => Report);

            [Tags("C")]
            public string Channel => GetArgument(() => Channel);
        }
    }
}