using System.Threading.Tasks;
using Gunter.Data.Configuration.Abstractions;

namespace Gunter.Services.Abstractions
{
    public interface IDispatchMessage
    {
        Task InvokeAsync(ITask task);
    }
}