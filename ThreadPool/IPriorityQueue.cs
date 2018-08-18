namespace ThreadPool
{
    internal interface IPriorityQueue
    {
        int Count { get; }
        bool Dequeue(out Work work);
        void Enqueue(Work work, QueueType priority);
    }
}