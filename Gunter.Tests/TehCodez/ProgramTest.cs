using System.Linq;
using Gunter.Messaging;
using Gunter.Tests.AutofacModules;
using Gunter.Tests.Messaging;
using Gunter.Tests.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
                        ["_Global.json"] = "InvalidGlobalsJson.json"
                    },
                    TestAlert = _testAlert
                }));

            Assert.AreEqual(5, exitCode);
            Assert.IsTrue(_memoryRecorder.Logs.Any(l => l.LogLevel() == LogLevel.Fatal));
        }

        [TestMethod]
        public void Start_InvalidTestJson_IgnoresInvalidFile()
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
        public void Start_InvalidSeverity_IgnoresInvalidTestCse()
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
        public void Start_FaultingDataSource_IgnoresSubsequentRuns()
        {
            var exitCode = Program.Start(
                new string[0],
                InitializeLogging,
                InitializeConfiguration,
                configuration => Program.InitializeContainer(configuration, new OverrideModule
                {
                    FileSystem = new TestFileSystem
                    {
                        ["test.json"] = "FaultingDataSource.json",
                    },
                    TestAlert = _testAlert
                }));

            Assert.AreEqual(0, exitCode);
            Assert.AreEqual(1, _memoryRecorder.Logs.Count(l => l.LogLevel() == LogLevel.Error));
            Assert.AreEqual(4, _memoryRecorder.Logs.Count(l => l.LogLevel() == LogLevel.Warn));
        }

        [TestMethod]
        public void Start_TestsWithProfiles_RunsOnlyMatchingTests()
        {
            var exitCode = Program.Start(
                new [] { "test1" },
                InitializeLogging,
                InitializeConfiguration,
                configuration => Program.InitializeContainer(configuration, new OverrideModule
                {
                    FileSystem = new TestFileSystem
                    {
                        ["test.json"] = "TestsWithProfiles.json",
                    },
                    TestAlert = _testAlert
                }));

            Assert.AreEqual(0, exitCode);
            Assert.AreEqual(0, _memoryRecorder.Logs.Count(l => l.LogLevel() == LogLevel.Error));
            Assert.AreEqual(2, _testAlert.GetReports("test").Count);
        }

        [TestMethod]
        public void Start_FiveTests_FiveAlerts()
        {
            var exitCode = Program.Start(
                new string[0],
                InitializeLogging,
                InitializeConfiguration,
                configuration => Program.InitializeContainer(configuration, new OverrideModule
                {
                    FileSystem = new TestFileSystem
                    {
                        ["test.json"] = "FiveTests.json",
                    },
                    TestAlert = _testAlert
                }));

            Assert.AreEqual(0, exitCode);
            Assert.AreEqual(0, _memoryRecorder.Logs.Count(l => l.LogLevel() == LogLevel.Error));
            Assert.AreEqual(5, _testAlert.GetReports("test").Count);
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
                new Memory
                {
                    {"Environment", "test"},
                    {"Workspace.Assets", "assets"},
                }
            });
        }
    }
}
