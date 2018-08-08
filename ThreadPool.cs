using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace packer
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

        public class Work
        {
            internal Delegate payload;
            internal IEnumerable<object> args;

            internal Work(Delegate payload, IEnumerable<object> args)
            {
                this.payload = payload;
                this.args = args;
            }

            private object result;
            private Work next;
            private QueueType priority;

            public object Result => this.result;
            public Work Next => this.next;
            internal QueueType Priority => this.priority;

            public Work Then(QueueType priority, Delegate payload, IEnumerable<object> args)
            {
                next = new Work(payload, args);
                next.SetPriority(priority);
                return next;
            }

            protected void SetPriority(QueueType priority)
            {
                this.priority = priority;
            }

            internal void Run()
            {
                result = payload.DynamicInvoke(args.ToArray());
                return;
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
            Console.WriteLine("Pool disposed...");
        }

        public void Wait()
        {
            SpinWait.SpinUntil(() => _queue.All(_ => _.Count == 0) && _workers.All(w => w.State == State.Idle));
        }

        public Work Queue(QueueType priority, Delegate payload, params object[] args)
        {
            if (_workers.Count < _threadCount)
            {
                var worker = new Task(new Thread(new ParameterizedThreadStart(Loop)));
                _workers.Add(worker);
                worker.Thread.Start(worker);
            }
            else
            {
                Task worker;
                if (_workers.TryTake(out worker))
                    if (worker.State != State.Stopped)
                        _workers.Add(worker);
            }
            var work = new Work(payload, args);
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
                        if(work.Next != null)
                        {
                            //if(work.Result != null)
                            //{
                                //var args = new List<object>(work.Next.args);
                                //args.Insert(0, work.Result);
                                //work.Next.args = args.ToArray();
                            //}
                            _queue[(int)work.Next.Priority - 1].Enqueue(work.Next);
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
