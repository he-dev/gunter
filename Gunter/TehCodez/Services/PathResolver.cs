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
    internal static class PathResolver
    {
        public static string ResolveFilePath(AppDomain appDomain, string fileName)
        {
            if (appDomain == null) throw new ArgumentNullException(nameof(appDomain));
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));

            if (Path.IsPathRooted(fileName))
            {
                return fileName;
            }

            return 
                GetLookupPaths(appDomain)
                    .Select(directoryName => Path.Combine(directoryName, fileName))
                    .FirstOrDefault(File.Exists);
        }

        public static string ResolveDirectoryPath(AppDomain appDomain, string subdirectoryName)
        {
            if (appDomain == null) throw new ArgumentNullException(nameof(appDomain));
            if (string.IsNullOrEmpty(subdirectoryName)) throw new ArgumentNullException(nameof(subdirectoryName));

            return
                GetLookupPaths(appDomain)
                    .Select(directoryName => Path.Combine(directoryName, subdirectoryName))
                    .FirstOrDefault(Directory.Exists);
        }

        private static IEnumerable<string> GetLookupPaths(AppDomain appDomain)
        {
            var lookupPaths = new List<string>
            {
                Path.GetDirectoryName(appDomain.SetupInformation.ConfigurationFile)
            };

            // Windows- and WebServices "hide" their paths somewhere else.
            if (!string.IsNullOrEmpty(appDomain.SetupInformation.PrivateBinPath))
            {
                lookupPaths.AddRange(appDomain.SetupInformation.PrivateBinPath.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            }

            return lookupPaths;
        }       
    }
}
