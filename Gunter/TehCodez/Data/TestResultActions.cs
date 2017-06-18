using System;

namespace Gunter.Data
{
    [Flags]
    public enum TestResultActions
    {
        None = 0,
        Halt = 1,
        Alert = 2,
    }
}