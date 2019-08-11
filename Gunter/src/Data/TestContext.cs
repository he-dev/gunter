using System.Data;
using Gunter.Services;

namespace Gunter.Data
{
    public class TestContext
    {
        public TestBundle TestBundle { get; set; }
        public TestCase TestCase { get; set; }
        //public TestWhen TestWhen { get; set; }
        public DataTable Data { get; set; }
        public ILog Log { get; set; }
        public RuntimePropertyProvider RuntimeProperties { get; set; }
        public string Query { get; set; }
        public TestResult Result { get; set; }
    }
}
