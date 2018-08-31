using SimpleLogger;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ThreadPool
{
    internal class PriorityQueue : IPriorityQueue
    {
        private Queue<Job>[] _queue;
        private object _sync;

        public int Count => _queue.Sum(_ => _.Count);

        public PriorityQueue()
        {
            _sync = new object();
            _queue = Enum.GetNames(typeof(QueueType)).Select(_ => new Queue<Job>()).ToArray();
        }

        public void Enqueue(Job job, QueueType priority)
        {
            lock(_sync)
                _queue[(int)priority].Enqueue(job);
            Logger.Log(Level.Verbose, $"job {job.Id} with {priority} was queued");
        }

        public bool Dequeue(out Job job)
        {
            job = Dequeue();
            var hasJob = job != null;
            
            if(!hasJob)
                Logger.Log(Level.Verbose, $"no jobs in the queue");
            else
                Logger.Log(Level.Verbose, $"dequeue {job.Id} from {job.Priority} queue");
            return hasJob;
        }

        private Job Dequeue()
        {
            Job work = null;
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
                Logger.Log(Level.Debug, "queue disposed");
            }
        }
    }
}
