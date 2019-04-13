namespace Gunter.Reporting
{
    public interface IFilter
    {
        object Apply(object data);
    }
}