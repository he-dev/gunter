using System.ComponentModel.DataAnnotations;
using System.Configuration;
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

        [Required]
        public string Environment => _configuration.GetValue(() => Environment);

        [Required]
        public string MailrBaseUri => _configuration.GetValue(() => MailrBaseUri);

        [Required]
        public string TestsDirectoryName => _configuration.GetValue(() => TestsDirectoryName);
    }
}