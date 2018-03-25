using System.Collections.Generic;
using Gunter.Data;

namespace Gunter
{
    internal interface ITestLoader
    {
        IEnumerable<TestFile> LoadTests(string path);
    }
}