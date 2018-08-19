using packer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace console
{
    

    


    class Program
    {
        //fsutil file createnew C:\testfile.txt 1000
        private static string source = @"C:\Users\John\source\repos\testfile.txt";
        private static string destination = @"C:\Users\John\source\repos\destination.txt";
        private static int chunkSize = 1 * 1024 * 1024; //1B * 1024 => 1KB * 1024 => 1MB
        private static long writeOffset = 0;
        private static long fileSize = 10 * chunkSize;
        private static int poolSize = 16;
        private static ThreadPool.ThreadPool _pool = new ThreadPool.ThreadPool(poolSize);
        
        private static ConcurrentBag<Chunk> _chuncksMetadata = new ConcurrentBag<Chunk>();
        private static object _fileExceedMaximumSize = new object();
        private static MemoryMappedFile _mmf;

        private static FileManager _manager;

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
                    Console.WriteLine("Writing threads finished working. Resizing file...");
                    fileSize += chunkSize * poolSize;
                    _mmf.Dispose();
                    _mmf = MemoryMappedFile.CreateFromFile(destination, FileMode.OpenOrCreate, "map", fileSize, MemoryMappedFileAccess.ReadWrite);
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
            var sourceInfo = new FileInfo(source);
            var sourceChunkCount = (int)Math.Ceiling(sourceInfo.Length / (decimal)chunkSize);

            SetFileSize(destination, fileSize);

            using (var manager = new FileManager(destination, poolSize))
            {
                for (var i = 0; i < sourceChunkCount; i++)
                    Compress(i);
                _pool.Wait();
                _pool.Dispose();
            }

            SetFileSize(destination, _manager.CurrentOffset);

            new MetadataWriter(destination).Write(_chuncksMetadata);
            _chuncksMetadata.Clear();

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

        private static void SetFileSize(string file, long size)
        {
            using (var fs = new FileStream(file, FileMode.OpenOrCreate))
                fs.SetLength(size);
        }

        private static int Compress(int index)
        {
            var read = _pool.Queue(ThreadPool.QueueType.Read, () => new ByteReader(source).Read(index, chunkSize * (long)index, chunkSize));
            var zip = read.Then(ThreadPool.QueueType.Zip, () => new Compressor().Zip(read.Result, index));
            zip.Then(ThreadPool.QueueType.Write, () => new ByteWriter(_manager).Write(zip.Result, index));
            zip.Then(ThreadPool.QueueType.Resize, () => ResizeFile());
            return index;
        }
    }
}
