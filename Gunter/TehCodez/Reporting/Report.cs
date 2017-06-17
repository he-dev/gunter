using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gunter.Data;
using Gunter.Services;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Gunter.Reporting
{
    [JsonObject]
    public interface IReport : IResolvable
    {
        [JsonRequired]
        int Id { get; set; }

        [JsonRequired]
        string Title { get; set; }

        [NotNull]
        [ItemNotNull]
        [JsonRequired]
        List<IModule> Modules { get; set; }
    }
    
    public class Report : IReport, IEnumerable<IModule>
    {
        private string _title;
        private IVariableResolver _variables = VariableResolver.Empty;

        [JsonIgnore]
        public IVariableResolver Variables
        {
            get => _variables;
            set
            {
                _variables = value;
                foreach (var section in Modules)
                {
                    section.UpdateVariables(value);
                }
            }
        }

        public int Id { get; set; }

        public string Title
        {
            get => Variables.Resolve(_title);
            set => _title = value;
        }

        public List<IModule> Modules { get; set; } = new List<IModule>();

        public IEnumerator<IModule> GetEnumerator() => Modules.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
