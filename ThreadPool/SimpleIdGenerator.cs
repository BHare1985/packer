using System;
using System.Threading;

namespace ThreadPool
{
    internal static class SimpleIdGenerator
    {
        private static long _next = 0;
        public static string GetId()
        {
            return Interlocked.Add(ref _next, 1).ToString();
        }
    }
}
