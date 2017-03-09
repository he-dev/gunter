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
        public static string Resolve(string subdirectoryName, string fileName)
        {
            return
                Path.IsPathRooted(fileName)
                    ? fileName
                    : Path.Combine(
                        Path.GetDirectoryName(Assembly.GetAssembly(typeof(Program)).Location),
                        subdirectoryName,
                        fileName
                    );
        }

        public static string Resolve(string fileName) => Resolve(string.Empty, fileName);
    }
}
