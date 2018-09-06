using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ThreadPool
{
    public class Job
    {
        internal Delegate _payload;

        internal Job(Delegate payload)
        {
            _payload = payload;
            Id = SimpleIdGenerator.GetId();
        }

        public string Id { get; }

        internal QueueType Priority { get; private set; }

        internal void SetPriority(QueueType priority)
        {
            Priority = priority;
        }

        internal Job Previous;

        internal void SetPreviousResult(Job result)
        {
            Previous = result;
        }

        internal virtual object Run()
        {
            var hasAnyParams = _payload.Method.GetParameters().Any();
            var result = hasAnyParams ? _payload.DynamicInvoke(Previous) : _payload.DynamicInvoke();
            Previous?.Disposing();
            return result;
        }

        private ConcurrentBag<Job> _next = new ConcurrentBag<Job>();
        internal IEnumerable<Job> Next => _next;

        internal void AddNext(Job job)
        {
            _next.Add(job);
        }

        protected virtual void Disposing() { }
    }
}
