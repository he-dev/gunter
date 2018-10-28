namespace Gunter.Reporting.Filters.Abstractions
{
    public interface IFilter
    {
        object Apply(object data);
    }
}