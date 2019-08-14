using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.IO;
using Microsoft.Extensions.Configuration;
using Reusable.IOnymous;
using Reusable.IOnymous.Config;
using Reusable.Quickey;

namespace Gunter
{
    [UseScheme("app"), UseMember]
    [PlainSelectorFormatter]
    public class ProgramInfo
    {
        private readonly IResourceSquid _resources;

        static ProgramInfo()
        {
            Configuration =
                new ConfigurationBuilder()
                    .SetBasePath(ProgramInfo.CurrentDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
        }

        public ProgramInfo(IResourceSquid resources)
        {
            _resources = resources;
        }

        public static IConfiguration Configuration { get; }

        public static string Name => "Gunter";

        public static string Version => "9.0.0";

        public static string FullName => $"{Name}-v{Version}";

        public static string CurrentDirectory => Path.GetDirectoryName(typeof(Program).Assembly.Location);

        public string Environment => _resources.ReadSetting(() => Environment);
    }

    [UseScheme("app"), UseMember]
    [TrimEnd("Config")]
    [PlainSelectorFormatter]
    public class ProgramConfig : SelectorBuilder<ProgramConfig>
    {
        public static Selector<string> Environment = Select(() => Environment);

        public static Selector<string> DefaultTestsDirectoryName = Select(() => DefaultTestsDirectoryName);
    }

    [UseScheme("mailr"), UseMember]
    [TrimEnd("Config")]
    [PlainSelectorFormatter]
    public class MailrConfig : SelectorBuilder<MailrConfig>
    {
        public static Selector<string> BaseUri = Select(() => BaseUri);

        public static Selector<string> TestResultPath = Select(() => TestResultPath);
    }
}