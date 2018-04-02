namespace Gunter.Reporting.Filters
{
    internal class Unchanged : IDataFilter
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