using packer;
using System;

namespace console
{
    class Program
    {
        //fsutil file createnew C:\testfile.txt 1000
        static void Main(string[] args)
        {
            string source = @"C:\Users\John\source\repos\testfile.txt";
            string destination = $@"C:\Users\John\source\repos\{Guid.NewGuid()}.txt";

            var settings = new CompressSettings { ChinkSize = 1 * 1024 * 1024 }; //1B * 1024 => 1KB * 1024 => 1MB 
            var strategy = new CompressStrategy(new CompressFactory(100), settings);
            strategy.Work(source, destination);

            //var meta = ReadMetadata(destination);
            //meta.Select(Decompres);

            Console.ReadLine();
        }

        //private static int Decompres(Chunk chunk)
        //{
            //var read = pool.Queue(ThreadPool.QueueType.Read, (RB)ReadBytes, chunk.Index, chunk.Offset, chunk.Size);
            //IEnumerable<object> ReadArgs() { yield return read.Result; yield return chunk.Index; }
            //var zip = read.Then(ThreadPool.QueueType.Zip, (ZP)Zip, ReadArgs());
            //return (int)chunk.Index;
        //}
    }
}
