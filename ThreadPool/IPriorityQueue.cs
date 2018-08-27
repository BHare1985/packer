using System;

namespace ThreadPool
{
    internal interface IPriorityQueue : IDisposable
    {
        int Count { get; }
        bool Dequeue(out Job work);
        void Enqueue(Job work, QueueType priority);
    }
}