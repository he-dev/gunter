using SmartConfig.Data.Annotations;

namespace Gunter.Data.Configurations
{
    [SmartConfig]
    [SettingName("Gunter")]
    public static class AppSettingsConfig
    {
        public static string Environment { get; set; }

        public static string TestsDirectoryName { get; set; }
    }
}