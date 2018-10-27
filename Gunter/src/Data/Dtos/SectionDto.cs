using System.Collections.Generic;

namespace Gunter.Data.Dtos
{
    public class SectionDto
    {
        public int Ordinal { get; set; }
        public string Heading { get; set; }
        public string Text { get; set; }
        
        public TripleTableDto Table { get; set; }
        //public IDictionary<string, IEnumerable<IEnumerable<object>>> Table { get; set; }

        public object Dump()
        {
            return new
            {
                Ordinal,
                Heading,
                Text,
                // todo - The rest-client cannot serialize it because it currently doesn't support custom serialization.
                Table = Table?.Dump()
            };
        }
    }
}