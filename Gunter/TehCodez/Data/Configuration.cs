using System.ComponentModel.DataAnnotations;

// ReSharper disable once CheckNamespace
namespace Gunter.Data.Configuration
{
    internal class Global
    {
        [Required]
        public string Environment { get; set; }

        [Required]
        public string TestsDirectoryName { get; set; }
    }
}
