using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gunter
{
    internal interface ITestFileProvider
    {
        IEnumerable<TestFileInfo> EnumerateTestFiles(string path);
    }

    internal class TestFileProvider : ITestFileProvider
    {
        public IEnumerable<TestFileInfo> EnumerateTestFiles(string path)
        {
            return
                Directory
                    .EnumerateFiles(path, "*.json")
                    .Select(testFileName => new TestFileInfo
                    {
                        Name = testFileName,
                        CreateReadStream = () => File.OpenRead(testFileName)
                    });
        }
    }

    internal class TestFileInfo
    {
        public string Name { get; set; }

        public Func<Stream> CreateReadStream { get; set; }
    }
}