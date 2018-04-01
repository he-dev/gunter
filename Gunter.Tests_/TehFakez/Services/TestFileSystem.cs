using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable;

namespace Gunter.Tests.Services
{
    [UsedImplicitly]
    internal class TestFileSystem : Dictionary<string, string>, IFileSystem
    {
        public TestFileSystem() : base(StringComparer.OrdinalIgnoreCase) { }

        public string WorkingDirectory { get; set; } = @"t:\tests\assets\targets";

        public bool Exists(string path)
        {
            return ContainsKey(Path.GetFileName(path));
        }

        public string ReadAllText(string fileName)
        {
            var actualFileName = this[Path.GetFileName(fileName)];
            var allText = ResourceReader.ReadEmbeddedResource<ProgramTest>($"Resources.assets.targets.{actualFileName}");
            if (string.IsNullOrEmpty(allText))
            {
                throw new FileNotFoundException($"File \"{fileName}\" does not exist.");
            }
            return allText;
        }

        public string[] GetFiles(string path, string searchPattern)
        {
            return Keys.Select(name => Path.Combine(WorkingDirectory, $"{name}")).ToArray();
        }
    }
}