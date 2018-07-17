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
    class Frame
    {
        public Frame(long size, long offset, long index)
        {
            Size = size;
            Offset = offset;
            Index = index;
        }
        public long Size { get; }
        public long Offset { get; }
        public long Index { get; }
    }
    class Program
    {
        private static volatile object sync = new object();

        //fsutil file createnew C:\testfile.txt 1000
        private static string source = @"C:\Users\John\source\repos\testfile.txt";
        private static string destination = @"C:\Users\John\source\repos\destination.txt";
        private static int chunkSize = 1 * 1024 * 1024; //1B * 1024 => 1KB * 1024 => 1MB
        private static long writeOffset = 0;
        private static long fileSize = 10 * chunkSize;

        private static ThreadPool pool = new ThreadPool(4);
        private static MemoryMappedFile mmf;
        private static ConcurrentBag<Frame> Map = new ConcurrentBag<Frame>();
        private static long writeCount = 0;
        private static ManualResetEvent _fileExceedMaximumSize = new ManualResetEvent(true);

        private delegate void RF();
        private static void ResizeFile()
        {
            try
            {
                if (!_fileExceedMaximumSize.WaitOne(TimeSpan.FromTicks(1)))
                {
                    Console.WriteLine("Resize process already started. Skiping.");
                    return;
                }
                _fileExceedMaximumSize.Reset();
                Console.WriteLine("Threads blocked for write. Waiting pending write threads to exit...");
                SpinWait.SpinUntil(() => Interlocked.Read(ref writeCount) == 0);
                Console.WriteLine("Writing threads finished working. Resizing file...");
                fileSize += chunkSize * 5;
                mmf.Dispose();
                //SetFileSize(destination, fileSize);
                mmf = MemoryMappedFile.CreateFromFile(destination, FileMode.OpenOrCreate, "map", fileSize, MemoryMappedFileAccess.ReadWrite);
                Console.WriteLine("File resized.");
            }
            finally
            {
                _fileExceedMaximumSize.Set();
            }
        }

        static void Main(string[] args)
        {
            var sourceInfo = new FileInfo(source);
            var sourceLength = sourceInfo.Length;
            var sourceChunkCount = (int)Math.Ceiling(sourceLength / (decimal)chunkSize);

            SetFileSize(destination, fileSize);

            mmf = MemoryMappedFile.CreateFromFile(destination, FileMode.OpenOrCreate, "map");
            Enumerable.Range(0, sourceChunkCount).Select(Process).ToArray();
            pool.Wait();
            mmf.Dispose();

            SetFileSize(destination, writeOffset);

            using (var fs = new FileStream(destination, FileMode.Append))
            {
                using(var writer = new BinaryWriter(fs))
                {
                    foreach(var point in Map)
                    {
                        writer.Write(point.Index);
                        writer.Write(point.Offset);
                        writer.Write(point.Size);
                    }
                    writer.Write(Map.Count);
                }
            }

            pool.Dispose();

            Console.ReadLine();
        }

        private static void SetFileSize(string file, long size)
        {
            using (var fs = new FileStream(file, FileMode.OpenOrCreate))
                fs.SetLength(size);
        }

        private static int Process(int index)
        {
            pool.Queue(3, (RB)ReadBytes, (object)index);
            return index;
        }

        private delegate void ZP(byte[] array, int index);

        private static void Zip(byte[] array, int index)
        {
            Console.WriteLine("{1} Zipping from: {0}", Thread.CurrentThread.ManagedThreadId, index);
            var output = new MemoryStream();
            var zip = new GZipStream(output, CompressionMode.Compress);
            new MemoryStream(array).CopyTo(zip);
            if (Interlocked.Read(ref writeOffset) + chunkSize * 5 > Interlocked.Read(ref fileSize))
                pool.Queue(4, (RF)ResizeFile);
            pool.Queue(1, (WB)WriteBytes, output.ToArray(), index);
        }

        private delegate void WB(byte[] array, int index);

        private static void WriteBytes(byte[] array, int index)
        {
            Console.WriteLine("{1} Writing bytes from: {0}", Thread.CurrentThread.ManagedThreadId, index);

            try
            {
                _fileExceedMaximumSize.WaitOne(Timeout.Infinite);
                Console.WriteLine("{0} nothing to wait for. Continue working...", Thread.CurrentThread.ManagedThreadId);

                Interlocked.Increment(ref writeCount);

                using (var file = MemoryMappedFile.OpenExisting("map"))
                {
                    var sum = Interlocked.Add(ref writeOffset, array.Length);
                    var offset = sum - array.Length;

                    using (var accestor = file.CreateViewAccessor(offset, array.Length))
                    {
                        accestor.WriteArray(0, array, 0, array.Length);
                    }
                    Map.Add(new Frame(array.Length, offset, index));
                }
            }
            finally
            {
                Interlocked.Decrement(ref writeCount);
            }
        }

        private delegate void RB(int index);

        private static void ReadBytes(int index)
        {
            Console.WriteLine("{1} Reading bytes from: {0}", Thread.CurrentThread.ManagedThreadId, index);

            using (var file = File.OpenRead(source))
            {
                long offset = chunkSize * (long)index;
                file.Seek(offset, SeekOrigin.Begin);
                var chunk = new byte[chunkSize];
                var size = file.Read(chunk, 0, chunkSize);
                pool.Queue(2, (ZP)Zip, chunk, index);
            }
        }

    }
}
