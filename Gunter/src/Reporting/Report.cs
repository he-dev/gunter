using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gunter.Data;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Gunter.Reporting
{
    [JsonObject]
    public interface IReport
    {
        [JsonRequired]
        int Id { get; set; }

        [JsonRequired]
        string Title { get; set; }

        [NotNull, ItemNotNull]
        [JsonRequired]
        List<IModule> Modules { get; set; }
    }
    
    public class Report : IReport, IEnumerable<IModule>
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public List<IModule> Modules { get; set; } = new List<IModule>();

        public IEnumerator<IModule> GetEnumerator() => Modules.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
