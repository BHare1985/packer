using System;

namespace SimpleLogger
{
    [Flags]
    public enum Level
    {
        None = 0,
        Info = 2 | Fatal,
        Verbose = 4 | Info,
        Fatal = 8,
        Debug = 16 | Verbose | Fatal,
    }
}
