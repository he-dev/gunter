using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Reusable.Data.Annotations;

namespace Gunter.Data
{
    internal class Workspace
    {
        [Ignore]
        public string AppName { get; } = TehApplicashun.Name;

        [Required]
        public string Environment { get; set; }

        [DefaultValue(nameof(Assets))]
        public string Assets { get; set; }

        [Ignore]
        public string Targets => Path.Combine(Assets, nameof(Targets));

        [Ignore]
        public string Themes => Path.Combine(Assets, nameof(Themes));
    }
}