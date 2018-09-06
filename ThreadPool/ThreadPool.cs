using SimpleLogger;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace ThreadPool
{
    public class ThreadPool : IDisposable
    {
        private readonly ConcurrentBag<Worker> _workers = new ConcurrentBag<Worker>();
        private readonly IPriorityQueue _queue;
        private readonly int _threadCount;
        private bool _disposed;
        
        internal ThreadPool(IPriorityQueue queue, int count)
        {
            _queue = queue;
            _threadCount = count;
        }

        public ThreadPool(int count) : this(new PriorityQueue(), count) { }

        public void Dispose()
        {
            Logger.Log(Level.Debug, "disposing thread pool");
            _disposed = true;
            Logger.Log(Level.Debug, "waiting for pending workers");
            SpinWait.SpinUntil(() => _workers.All(w => w.State == State.Stopped));
            Logger.Log(Level.Debug, "all workers finished their job");
            while (_workers.TryTake(out Worker worker)) { }
            Logger.Log(Level.Debug, "workers destroyed");
            _queue.Dispose();
            Logger.Log(Level.Debug, "thread pool disposed");
        }

        public Job<T> Queue<T>(QueueType priority, Func<T> payload)
        {
            Logger.Log(Level.Verbose, $"queuing new job with {priority} priority");
            if (_workers.Count < _threadCount && !_workers.Any(_ => _.State == State.Idle))
            {
                Logger.Log(Level.Verbose, $"all available workers are busy ({_workers.Count} of {_threadCount}) pool will be increased by new worker");
                var worker = new Worker(new Thread(new ParameterizedThreadStart(Loop)));
                _workers.Add(worker);
                worker.Thread.Start(worker);
                Logger.Log(Level.Verbose, $"new worker {worker.Id} added actual workers count {_workers.Count}");
            }

            var job = new Job<T>(payload);
            job.SetPriority(priority);
            _queue.Enqueue(job, priority);
            
            return job;
        }

        private void Loop(object o)
        {
            var idle = new AutoResetEvent(false);
            var worker = (Worker)o;
            while (!_disposed)
            {
                try
                {
                    Logger.Log(Level.Verbose, $"worker {worker.Id}: looking for new job");
                    if (_queue.Dequeue(out Job job))
                    {
                        Logger.Log(Level.Verbose, $"worker {worker.Id}: found new job {job.Id}");
                        worker.State = State.Working;
                        var result = job.Run();
                        Logger.Log(Level.Verbose, $"worker {worker.Id}: job {job.Id} finished");
                        if (job.Next != null && job.Next.Any())
                        {
                            Logger.Log(Level.Verbose, $"worker {worker.Id}: queuing continuation jobs for {job.Id}");
                            foreach (var next in job.Next)
                            {
                                _queue.Enqueue(next, next.Priority);
                                Logger.Log(Level.Verbose, $"job {next.Id} with {next.Priority} was queued");
                            }
                        }
                        Logger.Log(Level.Verbose, $"worker {worker.Id}: disposing finished job {job.Id}");
                    }
                    else
                    {
                        Logger.Log(Level.Verbose, $"worker {worker.Id}: nothing to do going to sleep for 1 second");
                        idle.Reset();
                        idle.WaitOne(TimeSpan.FromSeconds(1));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(Level.Fatal, $"worker {worker.Id}: failed when tried to do the job: {ex}");
                }
                worker.State = State.Idle;
            }
            Logger.Log(Level.Verbose, $"pool was disposed terminating worker {worker.Id}");
            worker.State = State.Stopped;
        }
    }
}
