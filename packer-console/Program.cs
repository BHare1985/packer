using CommandLine;
using packer;
using System;

namespace console
{
    partial class Program
    {
        //fsutil file createnew C:\testfile.txt 1000
        static void Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<CompressOptions, DecompressOptions>(args);
            result.WithParsed<DecompressOptions>(Decompress).WithParsed<CompressOptions>(Compress);
            Console.ReadLine();
        }

        private static void Compress(CompressOptions options)
        {
            var settings = new CompressSettings { ChunkSize = options.Chunksize * 1024 * 1024, PoolSize = options.Poolsize };
            IStrategy strategy = new CompressStrategy(settings);
            strategy.Work(options.Source, options.Destination);
        }

        private static void Decompress(DecompressOptions options)
        {
            IStrategy strategy = new DecompressStrategy(options.Poolsize);
            strategy.Work(options.Source, options.Destination);
        }
    }
}
