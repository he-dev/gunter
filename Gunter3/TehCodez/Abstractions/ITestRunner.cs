using System.Collections.Generic;
using System.Threading.Tasks;
using Gunter.Data;
using Reusable;

namespace Gunter
{
    public interface ITestRunner
    {
        Task RunTestsAsync(TestFile testFile, IEnumerable<SoftString> profiles);
    }
}