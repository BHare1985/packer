using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace console
{
    public class ThreadPool : IDisposable
    {
        private enum State
        {
            None,
            Working,
            Idle,
            Stopped
        }

        public enum QueueType
        {
            None,
            Resize,
            Write,
            Zip,
            Read,
        }


        private class Task
        {
            public Task(Thread thread)
            {
                Thread = thread;
            }
            public State  State { get; set; }
            public Thread Thread { get; }
        }

        public interface IWork<T>
        {
            T Result { get; }
        }


        public class Work<T> : Work, IWork<T>
        {
            internal Work(Delegate payload) : base(payload)
            {
                
            }

            private T result;
            public T Result => this.result;

            internal override void Run()
            {
                result = (T)payload.DynamicInvoke();
            }
        }

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

            virtual internal void Run()
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

            public Work<TResult> Then<TResult>(QueueType priority, Func<TResult> payload)
            {
                var job = new Work<TResult>(payload);
                job.SetPriority(priority);
                next.Add(job);
                return job;
            }

            public void Dispose()
            {
                payload = null;
                next.Clear();
                next = null;
            }
        }

        private ConcurrentQueue<Work>[] _queue = Enum.GetNames(typeof(QueueType)).Select(_ => new ConcurrentQueue<Work>()).ToArray();
        private ConcurrentBag<Task> _workers = new ConcurrentBag<Task>();
        private int _threadCount;
        private bool _disposed;
        

        public ThreadPool(uint count)
        {
            _threadCount = (int)count;
        }

        public void Dispose()
        {
            _disposed = true;
            SpinWait.SpinUntil(() => _workers.All(w => w.State == State.Stopped));
            _workers.Clear();
            Console.WriteLine("Pool disposed...");
        }

        public void Wait()
        {
            SpinWait.SpinUntil(() => _queue.All(_ => _.Count == 0) && _workers.All(w => w.State == State.Idle));
        }

        public Work<T> Queue<T>(QueueType priority, Func<T> payload)
        {
            if (_workers.Count < _threadCount)
            {
                var worker = new Task(new Thread(new ParameterizedThreadStart(Loop)));
                _workers.Add(worker);
                worker.Thread.Start(worker);
            }

            var work = new Work<T>(payload);
            _queue[(int)priority - 1].Enqueue(work);
            return work;
        }

        public Work Queue(QueueType priority, Delegate payload)
        {
            if (_workers.Count < _threadCount)
            {
                var worker = new Task(new Thread(new ParameterizedThreadStart(Loop)));
                _workers.Add(worker);
                worker.Thread.Start(worker);
            }

            var work = new Work(payload);
            _queue[(int)priority - 1].Enqueue(work);
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
                    Work work = null;
                    if (_queue.Any(_ => _.TryDequeue(out work)))
                    {
                        th.State = State.Working;
                        work.Run();
                        if (work.Next != null)
                        {
                            foreach (var next in work.Next)
                                _queue[(int)next.Priority - 1].Enqueue(next);
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
