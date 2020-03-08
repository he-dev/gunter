using System.Data;
using Gunter.Services;

namespace Gunter.Data
{
    public class TestContext
    {
        public TheoryFile TheoryFile { get; set; }
        public TestCase TestCase { get; set; }
        public DataTable Data { get; set; }
        public IQuery Query { get; set; }
        public RuntimePropertyProvider RuntimeProperties { get; set; }
        public string Command { get; set; }
        public TestResult Result { get; set; }
    }
}
