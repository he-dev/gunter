using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.IO;
using Reusable.IOnymous;
using Reusable.IOnymous.Config;
using Reusable.Quickey;

namespace Gunter
{
    [UseGlobal("app"), UseMember]
    [SettingSelectorFormatter]
    public class ProgramInfo
    {
        private readonly IResourceProvider _resources;

        public ProgramInfo(IResourceProvider resources)
        {
            _resources = resources;
        }

        public static string Name => "Gunter";

        public static string Version => "9.0.0";

        public static string FullName => $"{Name}-v{Version}";

        public static string CurrentDirectory => Path.GetDirectoryName(typeof(Program).Assembly.Location);

        public string Environment => _resources.ReadSetting(() => Environment);
    }

    [UseGlobal("app"), UseMember]
    [TrimStart("I"), TrimEnd("Config")]
    [SettingSelectorFormatter]
    public interface IProgramConfig
    {
        string Environment { get; set; }

        string DefaultTestsDirectoryName { get; set; }
    }

    [UseGlobal("mailr"), UseMember]
    [TrimStart("I"), TrimEnd("Config")]
    [SettingSelectorFormatter]
    public interface IMailrConfig
    {
        string BaseUri { get; set; }

        string TestResultPath { get; }
    }
}