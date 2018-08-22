using packer;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace console
{
    class Program
    {
        //fsutil file createnew C:\testfile.txt 1000
        private static string source = @"C:\Users\John\source\repos\testfile.txt";
        private static string destination = $@"C:\Users\John\source\repos\{Guid.NewGuid()}.txt";
        private static int chunkSize = 1 * 1024 * 1024; //1B * 1024 => 1KB * 1024 => 1MB
        private static int poolSize = 16;
        private static long fileSize = poolSize * chunkSize;

        private static ConcurrentBag<Chunk> _chuncksMetadata = new ConcurrentBag<Chunk>();

        static void Main(string[] args)
        {
            var sourceInfo = new FileInfo(source);
            var sourceChunkCount = (int)Math.Ceiling(sourceInfo.Length / (decimal)chunkSize);

            using (ThreadPool.ThreadPool pool = new ThreadPool.ThreadPool(poolSize))
            {
                using (var manager = new FileManager(destination, poolSize))
                {
                    for (var i = 0; i < sourceChunkCount; i++)
                    {
                        var index = i;
                        var read = pool.Queue(ThreadPool.QueueType.Read, () => new ByteReader(source).Read(index, chunkSize * (long)index, chunkSize));
                        var zip = read.Then(ThreadPool.QueueType.Zip, () => new Compressor().Zip(read.Result, index));
                        zip.Then(ThreadPool.QueueType.Write, () => _chuncksMetadata.Add(new ByteWriter(manager).Write(zip.Result, index)));
                    }
                    pool.Wait();
                }
            }

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
    }
}
