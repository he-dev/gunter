using JetBrains.Annotations;

namespace Gunter.Data.Abstractions
{
    [UsedImplicitly, PublicAPI]
    public interface IFormatData
    {
        string? Execute(object? value);
    }
}