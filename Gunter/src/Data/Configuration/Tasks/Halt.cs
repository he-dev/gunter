using Gunter.Annotations;
using Gunter.Data.Configuration.Abstractions;

namespace Gunter.Data.Configuration.Tasks
{
    [Gunter]
    public class Halt : ITask
    {
        public string? Name { get; set; }
    }
}