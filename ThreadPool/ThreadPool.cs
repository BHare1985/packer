using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace ThreadPool
{
    public class ThreadPool : IDisposable
    {
        private readonly ConcurrentBag<Task> _workers = new ConcurrentBag<Task>();
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
            _disposed = true;
            SpinWait.SpinUntil(() => _workers.All(w => w.State == State.Stopped));
            while(_workers.TryTake(out Task task)) { }
            _queue.Dispose();
            Console.WriteLine("Pool disposed...");
        }

        public void Wait()
        {
            SpinWait.SpinUntil(() => _queue.Count == 0 && _workers.All(w => w.State == State.Idle));
        }

        public WorkT<T> Queue<T>(QueueType priority, Func<T> payload)
        {
            if (_workers.Count < _threadCount)
            {
                var worker = new Task(new Thread(new ParameterizedThreadStart(Loop)));
                _workers.Add(worker);
                worker.Thread.Start(worker);
            }

            var work = new WorkT<T>(payload);
            _queue.Enqueue(work, priority);
            return work;
        }

        private void Loop(object o)
        {
            var idle = new AutoResetEvent(false);
            var th = (Task)o;
            while (!_disposed)
            {
                try
                {
                    if (_queue.Dequeue(out Work work))
                    {
                        th.State = State.Working;
                        work.Run();
                        if (work.Next != null)
                        {
                            foreach (var next in work.Next)
                                _queue.Enqueue(next, next.Priority);
                            work.Dispose();
                        }
                    }
                    else
                    {
                        idle.Reset();
                        idle.WaitOne(TimeSpan.FromSeconds(1));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("From {0}", Thread.CurrentThread.ManagedThreadId);
                    Console.WriteLine(ex);
                }
                th.State = State.Idle;
            }
            th.State = State.Stopped;
        }
    }
}
