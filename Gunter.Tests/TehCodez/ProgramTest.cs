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
using Reusable.Logging.Loggex;
using Reusable.Logging.Loggex.Recorders;
using Reusable.SmartConfig;
using Reusable.SmartConfig.Datastores;

namespace Gunter.Tests
{

    [TestClass]
    public class ProgramTest
    {
        private MemoryRecorder _memoryRecorder;
        private TestAlert _testAlert;

        [TestInitialize]
        public void TestInitialize()
        {
            _memoryRecorder = new MemoryRecorder();
            _testAlert = new TestAlert();
        }

        [TestMethod]
        public void Start_InvalidGlobalsJson_Exits()
        {
            var exitCode = Program.Start(
                new string[0],
                InitializeLogging,
                InitializeConfiguration,
                configuration => Program.InitializeContainer(configuration, new OverrideModule
                {
                    FileSystem = new TestFileSystem
                    {
                        ["_Global.json"] = "_InvalidGlobalsJson.json"
                    }
                }));

            Assert.AreEqual(5, exitCode);
            Assert.IsTrue(_memoryRecorder.Logs.Any(l => l.LogLevel() == LogLevel.Fatal));
        }

        [TestMethod]
        public void Start_InvalidTestJson_Ignores()
        {
            var exitCode = Program.Start(
                new string[0],
                InitializeLogging,
                InitializeConfiguration,
                configuration => Program.InitializeContainer(configuration, new OverrideModule
                {
                    FileSystem = new TestFileSystem
                    {
                        ["test1.json"] = "InvalidTestJson-ok.json",
                        ["test2.json"] = "InvalidTestJson-invalid.json",
                    },
                    TestAlert = _testAlert
                }));

            Assert.AreEqual(0, exitCode);
            Assert.AreEqual(1, _memoryRecorder.Logs.Count(l => l.LogLevel() == LogLevel.Error));
            Assert.AreEqual(1, _testAlert.GetReports("test1").Count);
        }

        [TestMethod]
        public void Start_InvalidSeverity_Ignores()
        {
            var exitCode = Program.Start(
                new string[0],
                InitializeLogging,
                InitializeConfiguration,
                configuration => Program.InitializeContainer(configuration, new OverrideModule
                {
                    FileSystem = new TestFileSystem
                    {
                        ["test.json"] = "InvalidSeverity.json",
                    },
                    TestAlert = _testAlert
                }));

            Assert.AreEqual(0, exitCode);
            Assert.AreEqual(1, _memoryRecorder.Logs.Count(l => l.LogLevel() == LogLevel.Error));
            Assert.AreEqual(0, _testAlert.GetReports("test1").Count);
        }

        [TestMethod]
        public void Start_FiveTests_FiveAlerts()
        {
            var exitCode = Program.Start(
                new string[0],
                Program.InitializeLogging,
                Program.InitializeConfiguration,
                configuration => Program.InitializeContainer(configuration, new OverrideModule
                {
                    FileSystem = new TestFileSystem
                    {
                        //TestFileNames = { "five-tests" }
                    }
                }));

            Assert.AreEqual(0, exitCode);
            //Assert.AreEqual(5, TestAlert.GetReports("five-tests").Count);
        }

        private void InitializeLogging()
        {
            Logger.Configuration = new LoggerConfiguration
            {
                //ComputedProperties = { new ElapsedSeconds(), new AppSetting(name: "Environment", key: "Environment") },
                Recorders = { _memoryRecorder },
                Filters =
                {
                    new LogFilter
                    {
                        LogLevel = LogLevel.Debug,
                        Recorders = { "Memory" }
                    }
                }
            };
        }

        private IConfiguration InitializeConfiguration()
        {
            return new Configuration(new[]
            {
                new Memory("Memory")
                {
                    {"Environment", "test"},
                    {"Workspace.Assets", "assets"},
                }
            });
        }
    }

    internal class OverrideModule : Module
    {
        public TestFileSystem FileSystem { get; set; }

        public TestAlert TestAlert { get; set; }

        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterInstance(TestAlert);

            builder
                .RegisterType<TestPathResolver>()
                .As<IPathResolver>();

            builder
                .RegisterInstance(FileSystem)
                .As<IFileSystem>();
        }
    }

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
