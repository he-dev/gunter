using System.Threading.Tasks;
using Gunter.Data.Configuration.Abstractions;
using JetBrains.Annotations;
using Reusable.OmniLog.Abstractions;

namespace Gunter.Services.Abstractions
{
    public interface IDispatchMessage
    {
        Task InvokeAsync(IMessage message);
    }
}