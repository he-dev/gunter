using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Gunter.Services
{
    public interface IPathResolver
    {
        string ResolveDirectoryPath(string subdirectoryName);
        string ResolveFilePath(string fileName);
    }

    [PublicAPI]
    internal class PathResolver : IPathResolver
    {
        private readonly AppDomain _appDomain;

        public PathResolver([NotNull] AppDomain appDomain)
        {
            _appDomain = appDomain ?? throw new ArgumentNullException(nameof(appDomain));
        }

        public PathResolver() : this(AppDomain.CurrentDomain) { }

        public string ResolveFilePath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));

            if (Path.IsPathRooted(fileName)) { return fileName; }

            return
                GetLookupPaths(_appDomain)
                    .Select(directoryName => Path.Combine(directoryName, fileName))
                    .FirstOrDefault(File.Exists);
        }

        public string ResolveDirectoryPath(string subdirectoryName)
        {
            if (string.IsNullOrEmpty(subdirectoryName)) throw new ArgumentNullException(nameof(subdirectoryName));

            if (Path.IsPathRooted(subdirectoryName)) { return subdirectoryName; }

            return
                GetLookupPaths(_appDomain)
                    .Select(directoryName => Path.Combine(directoryName, subdirectoryName))
                    .FirstOrDefault(Directory.Exists);
        }

        private static IEnumerable<string> GetLookupPaths(AppDomain appDomain)
        {
            yield return Path.GetDirectoryName(appDomain.SetupInformation.ConfigurationFile);
            yield return appDomain.BaseDirectory;
            yield return appDomain.SetupInformation.ApplicationBase;

            // Windows- and WebServices "hide" their actual paths somewhere else.
            var privateBinPaths = 
                appDomain
                    .SetupInformation
                    .PrivateBinPath
                    ?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries) 
                ?? Enumerable.Empty<string>();

            foreach (var path in privateBinPaths)
            {
                yield return path;
            }
        }
    }
}
