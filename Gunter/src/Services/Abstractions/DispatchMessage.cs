using System.Threading.Tasks;
using Gunter.Data.Configuration.Abstractions;
using Reusable.OmniLog.Abstractions;

namespace Gunter.Services.Abstractions
{
    public interface IDispatchMessage
    {
        Task InvokeAsync(IMessage message);
    }

    public abstract class DispatchMessage : IDispatchMessage
    {
        protected DispatchMessage(ILogger logger)
        {
            Logger = logger;
        }

        protected ILogger Logger { get; }

        public abstract Task InvokeAsync(IMessage message);
    }
}