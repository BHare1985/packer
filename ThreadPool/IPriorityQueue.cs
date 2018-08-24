using System;

namespace ThreadPool
{
    internal interface IPriorityQueue : IDisposable
    {
        int Count { get; }
        bool Dequeue(out Work work);
        void Enqueue(Work work, QueueType priority);
    }
}