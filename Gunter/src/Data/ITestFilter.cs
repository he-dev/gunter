using System.Collections.Generic;

namespace Gunter.Data
{
    public interface ITestFilter
    {
        string Path { get; }

        IList<string> Files { get; }

        IList<string> Tests { get; }

        IList<string> Tags { get; }
    }
}