using System;
using System.Threading;

namespace ThreadPool
{
    internal class Worker
    {
        public State State { get; set; }
        public Thread Thread { get; }
        public string Id { get; }

        public Worker(Thread thread)
        {
            Thread = thread;
            Id = SimpleIdGenerator.GetId();
        }
    }
}
