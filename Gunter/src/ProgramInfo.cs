using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.IO;
using Reusable.IOnymous;
using Reusable.SmartConfig;

namespace Gunter
{
    public class ProgramInfo
    {
        private readonly IConfiguration _configuration;

        public ProgramInfo(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static string Name => "Gunter";

        public static string Version => "6.0.0";

        public static string FullName => $"{Name}-v{Version}";

        public static string CurrentDirectory => Path.GetDirectoryName(typeof(Program).Assembly.Location);

        [Required]
        [SettingMember(Prefix = "app", Strength = SettingNameStrength.Low)]
        public string Environment => _configuration.GetSetting(() => Environment);

        [Required]
        [SettingMember(Prefix = "mailr", Strength = SettingNameStrength.Low)]
        public string MailrBaseUri => _configuration.GetSetting(() => MailrBaseUri);

        [Required]
        [SettingMember(Prefix = "app", Strength = SettingNameStrength.Low)]
        public string DefaultTestsDirectoryName => _configuration.GetSetting(() => DefaultTestsDirectoryName);
    }
}