using System.Collections.Generic;

namespace Gunter.Data
{
    public class TheoryFilter
    {
        public List<string> DirectoryNamePatterns { get; set; } = new List<string> { ".+" };
        public List<string> FileNamePatterns { get; set; } = new List<string> { ".+" };
        public List<string> TestNamePatterns { get; set; } = new List<string> { ".+" };
        public List<string> Tags { get; set; } = new List<string>();
    }
}