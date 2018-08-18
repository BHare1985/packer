using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ThreadPool
{
    public class Work : IDisposable
    {
        internal Delegate payload;

        internal Work(Delegate payload)
        {
            this.payload = payload;
        }

        private QueueType priority;

        internal QueueType Priority => this.priority;

        internal void SetPriority(QueueType priority)
        {
            this.priority = priority;
        }

        internal virtual void Run()
        {
            payload.DynamicInvoke();
        }

        private ConcurrentBag<Work> next = new ConcurrentBag<Work>();
        public IEnumerable<Work> Next => this.next;

        public Work Then(QueueType priority, Action payload)
        {
            var job = new Work(payload);
            job.SetPriority(priority);
            next.Add(job);
            return job;
        }

        public WorkT<TResult> Then<TResult>(QueueType priority, Func<TResult> payload)
        {
            var job = new WorkT<TResult>(payload);
            job.SetPriority(priority);
            next.Add(job);
            return job;
        }

        public void Dispose()
        {
            payload = null;
            while (next.TryTake(out Work work)) { }
            next = null;
        }
    }
}
