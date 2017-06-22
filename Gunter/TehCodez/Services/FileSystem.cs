using System.IO;
using JetBrains.Annotations;

namespace Gunter.Services
{
    [PublicAPI]
    internal interface IFileSystem
    {
        string ReadAllText(string fileName);
        string[] GetFiles(string path, string searchPattern);
    }

    internal class FileSystem : IFileSystem
    {
        public string ReadAllText(string fileName)
        {
            return File.ReadAllText(fileName);
        }

        public string[] GetFiles(string path, string searchPattern)
        {
            return Directory.GetFiles(path, searchPattern);
        }
    }
}