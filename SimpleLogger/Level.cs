using System;

namespace SimpleLogger
{
    [Flags]
    public enum Level
    {
        None = 0,
        Info = 2,
        Verbose = 4,
        Fatal = 8,
        Debug = 16,
    }
}
