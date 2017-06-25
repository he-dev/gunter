using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autofac;
using Gunter.Messaging;
using Gunter.Services;
using Gunter.Tests.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reusable;

namespace Gunter.Tests
{

    [TestClass]
    public class ProgramTest
    {
        [TestMethod]
        public void Start_FiveTests_FiveAlerts()
        {
            var exitCode = Program.Start(
                new string[0],
                Program.InitializeLogging,
                Program.InitializeConfiguration,
                configuration => Program.InitializeContainer(configuration, new TestModule
                {
                    FileSystem = new TestFileSystem
                    {
                        Files = { "five-tests" }
                    }
                }));

            Assert.AreEqual(0, exitCode);
            Assert.AreEqual(5, TestAlert.GetReports("five-tests").Count);
        }

        [TestMethod]
        public void Start_InvalidSeverity_NoAlerts()
        {
            var exitCode = Program.Start(
                new string[0],
                Program.InitializeLogging,
                Program.InitializeConfiguration,
                configuration => Program.InitializeContainer(configuration, new TestModule
                {
                    FileSystem = new TestFileSystem
                    {
                        Files = { "invalid-severity" }
                    }
                }));

            Assert.AreEqual(0, exitCode);
            Assert.AreEqual(0, TestAlert.GetReports("invalid-severity").Count);
        }
    }

    internal class TestModule : Autofac.Module
    {
        public TestFileSystem FileSystem { get; set; }

        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<TestAlert>();

            builder
                .RegisterType<TestPathResolver>()
                .As<IPathResolver>();

            builder
                .RegisterInstance(FileSystem)
                .As<IFileSystem>();
        }
    }

    internal class TestFileSystem : IFileSystem
    {
        public string WorkingDirectory { get; set; } = @"t:\tests\assets\targets";

        public List<string> Files { get; } = new List<string>();

        public string ReadAllText(string fileName)
        {
            //var fullName = Path.Combine(WorkingDirectory, $"{fileName}.json");

            if (Path.GetFileName(fileName).Equals("_Global.json", StringComparison.OrdinalIgnoreCase))
            {
                return ResourceReader.ReadEmbeddedResource<ProgramTest>("Resources.assets.targets._Global.json");
            }
            else
            {
                var json = ResourceReader.ReadEmbeddedResource<ProgramTest>($"Resources.assets.targets.{Path.GetFileName(fileName)}");
                if (string.IsNullOrEmpty(json))
                {
                    throw new FileNotFoundException($"File \"{fileName}\" does not exist.");
                }
                return json;
            }
        }

        public string[] GetFiles(string path, string searchPattern)
        {
            return Files.Select(name => Path.Combine(WorkingDirectory, $"{name}.json")).ToArray();
        }
    }

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
