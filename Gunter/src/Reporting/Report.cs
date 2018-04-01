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
    public interface IReport : IMergable
    {
        string Title { get; set; }

        [NotNull, ItemNotNull]        
        List<IModule> Modules { get; set; }
    }
    
    [JsonObject]
    public class Report : IReport, IEnumerable<IModule>
    {
        private readonly Factory _factory;

        public delegate Report Factory();

        public Report(Factory factory)
        {
            _factory = factory;
        }

        [JsonRequired]
        public int Id { get; set; }

        public string Merge { get; set; }

        [Mergable]
        public string Title { get; set; }

        [Mergable]
        public List<IModule> Modules { get; set; } = new List<IModule>();

        public IMergable New()
        {
            var mergable = _factory();
            mergable.Id = Id;
            mergable.Merge = Merge;
            return mergable;
        }

        public IEnumerator<IModule> GetEnumerator() => Modules.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
