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

        public static string Version => "8.0.0";

        public static string FullName => $"{Name}-v{Version}";

        public static string CurrentDirectory => Path.GetDirectoryName(typeof(Program).Assembly.Location);

        // todo - this needs to be refactored and removed from here
        [ResourcePrefix("app")]
        [ResourceName(Level = ResourceNameLevel.Member)]
        public string Environment => _configuration.GetItem(() => Environment);

//
//        [Required]
//        [SettingMember(Prefix = "mailr", Strength = SettingNameStrength.Low)]
        //public string MailrBaseUri => _configuration.GetSetting(() => MailrBaseUri);
//
//        [Required]
//        [SettingMember(Prefix = "app", Strength = SettingNameStrength.Low)]
        //public string DefaultTestsDirectoryName => _configuration.GetSetting(() => DefaultTestsDirectoryName);
    }

    [ResourcePrefix("app")]
    [ResourceName(Level = ResourceNameLevel.Member)]
    public interface IProgramConfig
    {
        string Environment { get; set; }


        string DefaultTestsDirectoryName { get; set; }
    }

    [ResourcePrefix("mailr")]
    [ResourceName(Level = ResourceNameLevel.Member)]
    public interface IMailrConfig
    {
        string BaseUri { get; set; }
        
        string TestResultPath { get; }
    }
}