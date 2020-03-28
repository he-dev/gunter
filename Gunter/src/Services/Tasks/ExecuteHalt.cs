using System;
using System.Threading.Tasks;
using Gunter.Data.Configuration.Tasks;
using Gunter.Services.Abstractions;

namespace Gunter.Services.Tasks
{
    public class ExecuteHalt : IExecuteTask<Halt>
    {
        public Task InvokeAsync(Halt task)
        {
            throw new OperationCanceledException();
        }
    }
}