using System.Collections.Generic;
using Gunter.Annotations;
using Reusable.Extensions;

namespace Gunter.Data.Configuration.Reports.CustomSections.Abstractions
{
    [Gunter]
    public abstract class CustomSection
    {
        public string Name => GetType().ToPrettyString();

        public HashSet<string> Tags { get; set; } = new HashSet<string>();
    }
}