using JetBrains.Annotations;

namespace Gunter.Reporting
{
    [UsedImplicitly, PublicAPI]
    public interface IFormatter
    {
        [CanBeNull]
        string Apply(object value);
    }
}