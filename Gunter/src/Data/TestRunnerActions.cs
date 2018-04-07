using System;

namespace Gunter.Data
{
    [Flags]
    public enum TestRunnerActions
    {
        None = 0,
        Halt = 1,
        Alert = 2,
    }
}