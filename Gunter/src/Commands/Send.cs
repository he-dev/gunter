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
    internal class Send : Command<Send.Parameter>
    {
        public Send(ILogger<Send> logger) { }

        protected override async Task ExecuteAsync(Parameter parameter, CancellationToken cancellationToken)
        {
            var messenger =
                parameter
                    .TestContext
                    .Theory
                    .Channels
                    .Where(m => m.Id.Equals(parameter.Channel))
                    .SingleOrThrow
                    (
                        onEmpty: () => DynamicException.Create("MessengerNotFound", $"Could not find messenger '{parameter.Channel}'.")
                    );

            // ReSharper disable once PossibleNullReferenceException - messenger won't be null
            await messenger.InvokeAsync(TODO, TODO, TODO);
        }

        [UsedImplicitly]
        public class Parameter : CommandParameter
        {
            [Tags("R")]
            public string Report { get; set; }

            [Tags("C")]
            public string Channel { get; set; }

            [Context]
            public TestContext TestContext { get; set; }
        }
    }
}