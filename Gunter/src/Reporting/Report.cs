using System.Collections;
using System.Collections.Generic;
using Gunter.Annotations;
using Gunter.Data;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;

namespace Gunter.Reporting
{
    public interface IReport : IPartial
    {
        string Title { get; set; }

        [NotNull, ItemNotNull]        
        List<IModule> Modules { get; set; }
    }
    
    [JsonObject]
    public class Report : IReport, IEnumerable<IModule>
    {
        [JsonRequired]
        public SoftString Id { get; set; }

        public Merge Merge { get; set; }

        [Mergeable]
        public string Title { get; set; }

        [Mergeable]
        public List<IModule> Modules { get; set; } = new List<IModule>();

        public IEnumerator<IModule> GetEnumerator() => Modules.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
