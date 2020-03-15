using System.Collections.Generic;
using Gunter.Data.Configuration.Abstractions;
using JetBrains.Annotations;

namespace Gunter.Data.Configuration
{
    [PublicAPI]
    [UsedImplicitly]
    public class TableOrView : Query<ITableOrView>, ITableOrView
    {
        public TemplateSelector TemplateSelectors { get; set; }

        public string ConnectionString { get; set; }

        public string Command { get; set; }

        public int Timeout { get; set; }
    }
}