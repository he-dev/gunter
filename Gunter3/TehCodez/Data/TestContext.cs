using System;
using System.Data;
using Gunter.Services;

namespace Gunter.Data
{
    public class TestContext
    {
        public TestFile TestFile { get; set; }
        public TestCase TestCase { get; set; }
        public IDataSource DataSource { get; set; }
        public DataTable Data { get; set; }
        public TimeSpan GetDataElapsed { get; set; }
        public TimeSpan RunTestElapsed { get; set; }
        public IRuntimeFormatter Formatter { get; set; }
        //public Func<string> Format { get; set; }
    }
}
