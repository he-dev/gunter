using System.Collections.Generic;
using System.Linq;
using Gunter.Data;

namespace Gunter.Extensions
{
    public static class TestBundleExtensions
    {
        public static IEnumerable<StaticProperty> Flatten(this IEnumerable<StaticPropertyCollection> variables)
        {
            return variables.SelectMany(x => x);
        }
    }
}