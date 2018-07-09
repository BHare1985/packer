using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace packer
{
    class Program
    {
        private static volatile object sync = new object();

        //fsutil file createnew C:\testfile.txt 1000
        private static string source = @"C:\Users\John\Documents\bomb.txt";
        private static string destination = @"C:\Users\John\Documents\destination.txt";
        private static int chunkSize = 1024 * 1024;
        private static int writeOffset = 0;

        private static ThreadPool pool = new ThreadPool(10);

        static void Main(string[] args)
        {
            var sourceInfo = new FileInfo(source);
            var sourceLength = sourceInfo.Length;
            var sourceChunkCount = (int)Math.Ceiling(sourceLength / (decimal)chunkSize);

            var fs = new FileStream(destination, FileMode.Create);
            fs.Seek(10000, SeekOrigin.Begin);
            fs.WriteByte(0);
            fs.Close();

            var mmf = MemoryMappedFile.CreateFromFile(destination, FileMode.OpenOrCreate, "map");

            Enumerable.Range(0, sourceChunkCount).Select(Process).ToArray();

            //Enumerable.Range(0, 100).Select(_ => { pool.Queue(() => Console.WriteLine("Print {0} from: {1}", _, Thread.CurrentThread.ManagedThreadId)); return _; }).ToArray();
            //pool.Wait();
            //pool.Dispose();
            Console.ReadLine();
            mmf.Dispose();
        }

        private static int Process(int index)
        {
            pool.Queue(() => ReadBytes(index), 3);
            return index;
        }

        private static void Zip(byte[] array, int index)
        {
            Console.WriteLine("{1} Zipping from: {0}", Thread.CurrentThread.ManagedThreadId, index);
            var output = new MemoryStream();
            var zip = new GZipStream(output, CompressionMode.Compress);
            new MemoryStream(array).CopyTo(zip);
            pool.Queue(() => WriteBytes(output.ToArray(), index), 1);
        }

        private static void WriteBytes(byte[] array, int index)
        {
            Console.WriteLine("{1} Writing bytes from: {0}", Thread.CurrentThread.ManagedThreadId, index);
            using (var mmf = MemoryMappedFile.OpenExisting("map"))
            {
                var dto = new ZipedData(array, index);
                using (var accestor = mmf.CreateViewAccessor(writeOffset, Marshal.SizeOf<ZipedData>()))
                {
                    accestor.Write(0, ref dto);
                }
            }
        }

        private static void ReadBytes(int index)
        {
            Console.WriteLine("{1} Reading bytes from: {0}", Thread.CurrentThread.ManagedThreadId, index);

            using (var file = File.OpenRead(source))
            {
                var offset = chunkSize * index;
                file.Seek(offset, SeekOrigin.Begin);
                var chunk = new byte[chunkSize];
                var size = file.Read(chunk, 0, chunkSize);
                pool.Queue(() => Zip(chunk, index), 2);
            }
        }

    }

    public class Scheduler
    {
        public Scheduler()
        {

        }


    }

    public class ThreadPool : IDisposable
    {
        private ConcurrentQueue<Action> _read = new ConcurrentQueue<Action>();
        private ConcurrentQueue<Action> _zip = new ConcurrentQueue<Action>();
        private ConcurrentQueue<Action> _write = new ConcurrentQueue<Action>();
        private ConcurrentBag<Thread> _workers = new ConcurrentBag<Thread>();
        private object _sync = new object();
        private int _threadCount;
        private bool _disposed;

        public ThreadPool(uint count)
        {
            _threadCount = (int)count;
        }

        public void Wait()
        {
            SpinWait.SpinUntil(() => _read.Count == 0 && _zip.Count == 0 && _write.Count == 0);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if(disposing)
            {
                Console.WriteLine("Clearing queue");
                _read.Clear();
            }

            _disposed = true;
        }

        ~ThreadPool()
        {
            Dispose(false);
        }

        public void Queue(Action payload, int type)
        {
            if (_workers.Count < _threadCount)
            {
                var worker = new Thread(new ThreadStart(Loop));
                _workers.Add(worker);
                worker.Start();
            }
            //var work = new Work<TOut>(payload);
            switch (type)
            {
                case 1: _write.Enqueue(payload); break;
                case 2: _zip.Enqueue(payload); break;
                case 3: _read.Enqueue(payload); break;
            }
        }

        

        private void Loop()
        {
            while (!_disposed)
            {
                try
                {
                    Action work;
                    if (_write.TryDequeue(out work) || _zip.TryDequeue(out work) || _read.TryDequeue(out work))
                        work();
                    else Thread.SpinWait(10);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("From {0}", Thread.CurrentThread.ManagedThreadId);
                    Console.WriteLine(ex);
                }
            }
        }

        internal Work<T> Create<T>(Func<T> p)
        {
            return new Work<T>(p);
        }
    }

    public class Work
    {
        protected readonly Delegate work;

        public Work(Delegate work)
        {
            this.work = work;
        }

        internal void Run()
        {
            InternalRun();
        }

        protected virtual void InternalRun()
        {
            work.DynamicInvoke();
        }
    }

    public class Work<T> : Work
    {
        public Work(Func<T> action) : base(action) { }
        public Work(Action<T> action) : base(action) { }

        protected override void InternalRun()
        {
            _result = (T)work.DynamicInvoke();
        }

        private T _result;

        public T Result()
        {
            return _result;
        }
    }

    public static class WorkExtension
    {
        public static Work<TOut> Then<TIn, TOut>(this Work<TIn> current, Func<TIn, TOut> then)
        {
            return new Work<TOut>(() => then(current.Result()));
        }

        public static Work Then<TIn>(this Work<TIn> current, Action<TIn> then)
        {
            return new Work((Action)(() => then(current.Result())));
        }
    }
}
