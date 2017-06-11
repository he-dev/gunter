using System.ComponentModel.DataAnnotations;
using Reusable.ConfigWhiz.Data.Annotations;

// ReSharper disable once CheckNamespace
namespace Gunter.Data.Configuration
{
    [SettingName("Config")]
    internal class ProgramConfig
    {
        [Required]
        public string Environment { get; set; }

        [Required]
        public string TestsDirectoryName { get; set; }
    }
}
