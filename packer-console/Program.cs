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
            //check source file exists
            //check read permissions for source file
            //check destination file dos not exists
            //check write permissions
            //check disk capacity //compress => warn for low disk //decompress => fail low disk
            Console.ReadLine();
        }

        private static void Compress(CompressOptions options)
        {
            var settings = new CompressSettings { ChunkSize = ConvertMegabytesToBytes(options), PoolSize = options.Poolsize };
            IStrategy strategy = new CompressStrategy(settings);
            strategy.Work(options.Source, options.Destination);
        }

        private static int ConvertMegabytesToBytes(CompressOptions options)
        {
            var kbytes = options.Chunksize * 1024;
            var bytes = kbytes * 1024;
            return bytes;
        }

        private static void Decompress(DecompressOptions options)
        {
            IStrategy strategy = new DecompressStrategy(options.Poolsize);
            strategy.Work(options.Source, options.Destination);
        }
    }
}
