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
    class Chunk
    {
        public Chunk(long size, long offset, long index)
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
        private static int poolSize = 16;
        private static ThreadPool pool = new ThreadPool((uint)poolSize);
        private static MemoryMappedFile mmf;
        private static ConcurrentBag<Chunk> ChuncksMetadata = new ConcurrentBag<Chunk>();
        private static long writeCount = 0;
        private static object _fileExceedMaximumSize = new object();
        private static SemaphoreSlim _resize = new SemaphoreSlim(poolSize, poolSize);

        private static void ResizeFile()
        {
            if (fileSize > Interlocked.Read(ref writeOffset) + chunkSize * poolSize) return;
            
            if (Monitor.IsEntered(_fileExceedMaximumSize)) return;
            try
            {
                lock (_fileExceedMaximumSize)
                {
                    Console.WriteLine("Threads blocked for a write. Waiting pending write threads to exit...");
                    Enumerable.Range(0, poolSize).Select(_ => { _resize.Wait(); return _; }).ToArray();
                    SpinWait.SpinUntil(() => Interlocked.Read(ref writeCount) == 0);
                    Console.WriteLine("Writing threads finished working. Resizing file...");
                    fileSize += chunkSize * poolSize;
                    mmf.Dispose();
                    //SetFileSize(destination, fileSize);
                    mmf = MemoryMappedFile.CreateFromFile(destination, FileMode.OpenOrCreate, "map", fileSize, MemoryMappedFileAccess.ReadWrite);
                    Console.WriteLine("File resized.");
                }
                
            }
            finally
            {
                _resize.Release(poolSize);
            }

        }

        static void Main(string[] args)
        {
            Console.ReadLine();
            var sourceInfo = new FileInfo(source);
            var sourceLength = sourceInfo.Length;
            var sourceChunkCount = (int)Math.Ceiling(sourceLength / (decimal)chunkSize);

            SetFileSize(destination, fileSize);

            mmf = MemoryMappedFile.CreateFromFile(destination, FileMode.OpenOrCreate, "map");
            for(var i = 0; i < sourceChunkCount; i++)
                Compress(i);
            pool.Wait();
            mmf.Dispose();
            pool.Dispose();

            SetFileSize(destination, writeOffset);

            WriteMetadata(destination, ChuncksMetadata);

            ChuncksMetadata.Clear();
            //var meta = ReadMetadata(destination);
            //meta.Select(Decompres);
            
            Console.ReadLine();
        }

        private static int Decompres(Chunk chunk)
        {
            //var read = pool.Queue(ThreadPool.QueueType.Read, (RB)ReadBytes, chunk.Index, chunk.Offset, chunk.Size);
            //IEnumerable<object> ReadArgs() { yield return read.Result; yield return chunk.Index; }
            //var zip = read.Then(ThreadPool.QueueType.Zip, (ZP)Zip, ReadArgs());
            return (int)chunk.Index;
        }

        private static IEnumerable<Chunk> ReadMetadata(string file)
        {
            var info = new FileInfo(file);
            
            using (var fs = new FileStream(file, FileMode.Open))
            {
                using (var reader = new BinaryReader(fs))
                {
                    fs.Seek(info.Length - Marshal.SizeOf<long>(), SeekOrigin.Begin);
                    var count = reader.ReadInt32();
                    var metaOffset = count * (Marshal.SizeOf<long>() + Marshal.SizeOf<long>() + Marshal.SizeOf<long>()) + Marshal.SizeOf<long>();
                    fs.Seek(info.Length - metaOffset, SeekOrigin.Begin);
                    var chunks = new List<Chunk>(count);
                    for(var i = 0; i < count; i++)
                        chunks.Add(new Chunk(reader.ReadInt64(), reader.ReadInt64(), reader.ReadInt64()));
                    return chunks;
                }
            }
        }

        private static void WriteMetadata(string file, IEnumerable<Chunk> chunks)
        {
            using (var fs = new FileStream(destination, FileMode.Append))
            {
                using (var writer = new BinaryWriter(fs))
                {
                    long count = 0;
                    foreach (var chunk in chunks)
                    {
                        writer.Write(chunk.Size);
                        writer.Write(chunk.Offset);
                        writer.Write(chunk.Index);
                        count++;
                    }
                    writer.Write(count);
                }
            }
        }

        private static void SetFileSize(string file, long size)
        {
            using (var fs = new FileStream(file, FileMode.OpenOrCreate))
                fs.SetLength(size);
        }

        private static int Compress(int index)
        {
            var read = pool.Queue(ThreadPool.QueueType.Read, () => ReadBytes(index, chunkSize * (long)index, chunkSize));
            var zip = read.Then(ThreadPool.QueueType.Zip, () => Zip(read.Result, index));
            var write = zip.Then(ThreadPool.QueueType.Write, () => WriteBytes(zip.Result, index));
                        zip.Then(ThreadPool.QueueType.Resize, () => ResizeFile());
            return index;
        }

        private static byte[] Zip(byte[] array, int index)
        {
            Console.WriteLine("{1} Zipping from: {0}", Thread.CurrentThread.ManagedThreadId, index);
            var output = new MemoryStream();
            var zip = new GZipStream(output, CompressionMode.Compress);
            new MemoryStream(array).CopyTo(zip);
            return output.ToArray();
        }

        private static void WriteBytes(byte[] array, int index)
        {
            Console.WriteLine("{1} Writing bytes from: {0}", Thread.CurrentThread.ManagedThreadId, index);

            try
            {
                _resize.Wait();
               
                Console.WriteLine("{0} nothing to wait for. Continue working...", Thread.CurrentThread.ManagedThreadId);

                using (var file = MemoryMappedFile.OpenExisting("map"))
                {
                    var sum = Interlocked.Add(ref writeOffset, array.Length);
                    var offset = sum - array.Length;

                    using (var accestor = file.CreateViewAccessor(offset, array.Length))
                    {
                        accestor.WriteArray(0, array, 0, array.Length);
                    }
                    ChuncksMetadata.Add(new Chunk(array.Length, offset, index));
                }
            }
            finally
            {
                _resize.Release();
                Console.WriteLine("{0} Write of {1} bytes finished on index {2}", Thread.CurrentThread.ManagedThreadId, array.Length, index);
            }
        }

        private static byte[] ReadBytes(int index, long offset, int length)
        {
            Console.WriteLine("{1} Reading bytes from: {0}", Thread.CurrentThread.ManagedThreadId, index);

            using (var file = File.OpenRead(source))
            {
                file.Seek(offset, SeekOrigin.Begin);
                var chunk = new byte[length];
                var size = file.Read(chunk, 0, length);
                return chunk;
            }
        }

    }
}
