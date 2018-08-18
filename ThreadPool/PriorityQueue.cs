using System;
using System.Collections.Generic;
using System.Linq;

namespace ThreadPool
{
    internal class PriorityQueue : IDisposable, IPriorityQueue
    {
        private Queue<Work>[] _queue;
        private object _sync;

        public int Count => _queue.Sum(_ => _.Count);

        public PriorityQueue()
        {
            _sync = new object();
            _queue = Enum.GetNames(typeof(QueueType)).Select(_ => new Queue<Work>()).ToArray();
        }

        public void Enqueue(Work work, QueueType priority)
        {
            lock(_sync)
                _queue[(int)priority - 1].Enqueue(work);
        }

        public bool Dequeue(out Work work)
        {
            work = Dequeue();
            return work != null;
        }

        private Work Dequeue()
        {
            Work work = null;
            lock(_sync)
            {
                var queue = _queue.FirstOrDefault(_ => _.Count > 0);
                if(queue != null)
                    work = queue.Dequeue();
            }
            return work;
        }

        public void Dispose()
        {
            lock(_sync)
            {
                foreach (var queue in _queue)
                    while (queue.Count > 0)
                        queue.Dequeue();
                _queue = null;
            }
        }
    }
}
