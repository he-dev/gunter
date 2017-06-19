namespace Gunter.Reporting.Filters
{
    internal class Unchanged : IDataFilter
    {
        public object Apply(object data)
        {
            return data;
        }
    }
}