using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Gunter.Reporting
{
    public interface IReport
    {
        [JsonRequired]
        int Id { get; set; }

        [JsonRequired]
        string Title { get; set; }

        [JsonRequired]
        List<ISection> Sections { get; set; }
    }

    public class Report : IReport
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public List<ISection> Sections { get; set; }
    }
}
