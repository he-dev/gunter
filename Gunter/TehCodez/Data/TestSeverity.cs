using JetBrains.Annotations;

namespace Gunter.Data
{
    [PublicAPI]
    public enum TestSeverity
    {
        Debug,
        Info,
        Warn,
        Error,
        Critical,
        None,
    }
}
