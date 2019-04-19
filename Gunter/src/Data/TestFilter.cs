using System.Collections.Generic;

namespace Gunter.Data
{
    public class TestFilter
    {
        public string Path { get; set; }
        
        public IList<string> Files { get;set; }
        
        public IList<string> Tests { get;set; }
        
        public IList<string> Tags { get; set;}
    }
}