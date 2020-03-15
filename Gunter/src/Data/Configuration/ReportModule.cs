using System.Collections.Generic;
using Reusable.Extensions;

namespace Gunter.Data.Configuration
{
    public abstract class ReportModule
    {
        public string Name => GetType().ToPrettyString();

        public HashSet<string> Tags { get; set; } = new HashSet<string>();
    }
}