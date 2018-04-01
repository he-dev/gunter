using System.IO;
using Gunter.Services;
using JetBrains.Annotations;

namespace Gunter.Tests.Services
{
    [UsedImplicitly]
    internal class TestPathResolver : IPathResolver
    {
        public string ResolveDirectoryPath(string subdirectoryName)
        {
            //return subdirectoryName;
            return Path.Combine(@"t:\tests", subdirectoryName);
        }

        public string ResolveFilePath(string fileName)
        {
            return fileName;
            //return @"t:\tests";
        }
    }
}