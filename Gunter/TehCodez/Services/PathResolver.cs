using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Gunter.Services
{
    internal class PathResolver
    {
        public static string Resolve(string subdirectoryName, string fileName) =>
            Path.IsPathRooted(fileName) ? fileName : Path.Combine(
                Environment.CurrentDirectory,
                subdirectoryName ?? throw new ArgumentNullException(nameof(subdirectoryName)),
                fileName ?? throw new ArgumentNullException(nameof(fileName))
            );

        public static string Resolve(string fileName) => Resolve(string.Empty, fileName);
    }
}
