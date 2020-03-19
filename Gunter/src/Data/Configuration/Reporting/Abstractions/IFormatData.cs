using JetBrains.Annotations;

namespace Gunter.Data.Configuration.Reporting.Abstractions
{
    [UsedImplicitly, PublicAPI]
    public interface IFormatData
    {
        string? Execute(object? value);
    }
}