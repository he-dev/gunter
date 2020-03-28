using Gunter.Data.Abstractions;
using JetBrains.Annotations;

namespace Gunter.Data.Configuration.Queries
{
    [PublicAPI]
    [UsedImplicitly]
    public class TableOrView : Query, IMergeable
    {
        public ModelSelector? ModelSelector { get; set; }

        //[JsonRequired]
        public string? ConnectionString { get; set; }

        //[JsonRequired]
        public string? Command { get; set; }

        public int Timeout { get; set; }
    }
}