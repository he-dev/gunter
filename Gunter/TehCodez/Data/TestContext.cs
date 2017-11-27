﻿using System.Data;

namespace Gunter.Data
{
    public class TestContext
    {
        public TestFile TestFile { get; set; }
        public TestCase TestCase { get; set; }
        public IDataSource DataSource { get; set; }
        public DataTable Data { get; set; }
        public IRuntimeFormatter Formatter { get; set; }
    }
}
