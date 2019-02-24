using JetBrains.Annotations;

namespace Gunter.Reporting.Formatters.Abstractions
{
    [UsedImplicitly, PublicAPI]
    public interface IFormatter
    {
        string Apply(object value);
    }
}