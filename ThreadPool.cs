using System;
using System.Collections.Concurrent;
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

        private enum QueueType
        {
            None,
            Write,
            Zip,
            Read
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

        private class Work
        {
            private Delegate payload;
            private object[] args;

            public Work(Delegate payload, object[] args)
            {
                this.payload = payload;
                this.args = args;
            }

            public QueueType Queue { get; internal set; }

            public void Run()
            {
                payload.DynamicInvoke(args);
            }
        }


        private ConcurrentQueue<Work> _read = new ConcurrentQueue<Work>();
        private ConcurrentQueue<Work> _zip = new ConcurrentQueue<Work>();
        private ConcurrentQueue<Work> _write = new ConcurrentQueue<Work>();
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
            SpinWait.SpinUntil(() => _read.Count == 0 && _zip.Count == 0 && _write.Count == 0 && _workers.All(w => w.State == State.Idle));
        }

        public void Queue(int priority, Delegate payload, params object[] args)
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
            switch (priority)
            {
                case 1: work.Queue = QueueType.Write; _write.Enqueue(work); break;
                case 2: work.Queue = QueueType.Zip; _zip.Enqueue(work); break;
                case 3: work.Queue = QueueType.Read; _read.Enqueue(work); break;
            }
        }

        private void Loop(object o)
        {
            var idle = new AutoResetEvent(false);
            var th = (Task)o;
            while (!_disposed)
            {
                try
                {
                    Work work;
                    if (_write.TryDequeue(out work) || _zip.TryDequeue(out work) || _read.TryDequeue(out work))
                    {
                        th.State = State.Working;
                        work.Run();
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
