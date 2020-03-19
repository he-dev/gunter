using System.Collections.Generic;
using Gunter.Annotations;
using Reusable.Extensions;

namespace Gunter.Data.Configuration.Abstractions
{
    [Gunter]
    public abstract class ReportModule
    {
        public string Name => GetType().ToPrettyString();

        public HashSet<string> Tags { get; set; } = new HashSet<string>();
    }
}