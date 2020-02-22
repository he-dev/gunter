using JetBrains.Annotations;

namespace Gunter.Reporting
{
    [UsedImplicitly, PublicAPI]
    public interface IFormatter
    {
        string? Apply(object? value);
    }
}