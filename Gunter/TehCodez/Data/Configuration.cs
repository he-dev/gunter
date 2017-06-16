using System.ComponentModel.DataAnnotations;
using Reusable.ConfigWhiz.Data.Annotations;

// ReSharper disable once CheckNamespace
namespace Gunter.Data.Configuration
{
    internal class Context
    {
        [Required]
        public string Environment { get; set; }
    }

    internal class Workspace
    {
        [Required]
        public string TestsDirectoryName { get; set; }
    }
}
