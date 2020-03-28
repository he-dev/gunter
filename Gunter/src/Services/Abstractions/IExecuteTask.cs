using System.Threading.Tasks;
using Gunter.Data.Configuration.Abstractions;

namespace Gunter.Services.Abstractions
{
    public interface IExecuteTask<in T> where T : ITask
    {
        Task InvokeAsync(T task);
    }
}