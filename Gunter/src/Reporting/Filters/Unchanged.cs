using Gunter.Reporting.Filters.Abstractions;

namespace Gunter.Reporting.Filters
{
    internal class Unchanged : IFilter
    {
        public object Apply(object data)
        {
            return data;
        }

        public override string ToString()
        {
            return $"{nameof(Unchanged)}";
        }
    }
}