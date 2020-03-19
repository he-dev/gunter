using System.IO;
using Microsoft.Extensions.Configuration;
using Reusable.Quickey;
using Reusable.Translucent;

namespace Gunter
{
    [UseScheme("app"), UseMember]
    public class ProgramInfo
    {
        private readonly IResource _resource;

        static ProgramInfo()
        {
            Configuration =
                new ConfigurationBuilder()
                    .SetBasePath(ProgramInfo.CurrentDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
        }

        public ProgramInfo(IResource resource)
        {
            _resource = resource;
        }

        public static IConfiguration Configuration { get; }

        public static string Name => "Gunter";

        public static string Version => "11.0.0";

        public static string FullName => $"{Name}-v{Version}";

        public static string CurrentDirectory => Path.GetDirectoryName(typeof(Program).Assembly.Location);

        public string Environment => _resource.ReadSetting(() => Environment);
    }

    public class RuntimeInfo
    {
        
    }

    [UseScheme("app"), UseMember]
    [TrimEnd("Config")]
    public class ProgramConfig : SelectorBuilder<ProgramConfig>
    {
        public static Selector<string> Environment = Select(() => Environment);

        public static Selector<string> DefaultTestsDirectoryName = Select(() => DefaultTestsDirectoryName);
    }

    [UseScheme("mailr"), UseMember]
    [TrimEnd("Config")]
    public class MailrConfig : SelectorBuilder<MailrConfig>
    {
        public static Selector<string> BaseUri = Select(() => BaseUri);

        public static Selector<string> TestResultPath = Select(() => TestResultPath);
    }
}