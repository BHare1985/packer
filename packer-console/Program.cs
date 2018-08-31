using CommandLine;
using packer;
using System;
using SimpleLogger;
using System.Diagnostics;

namespace console
{
    partial class Program
    {
        //fsutil file createnew C:\testfile.txt 1000
        static void Main(params string[] args)
        {
            try
            {
                var result = Parser.Default.ParseArguments<CompressOptions, DecompressOptions>(args);
                result.WithParsed<DecompressOptions>(Decompress).WithParsed<CompressOptions>(Compress);
            }
            catch (ArgumentNullException ex)
            {
                Console.WriteLine("please provide arguments");
            }
            //check source file exists
            //check read permissions for source file
            //check destination file dos not exists //rewrite?
            //check write permissions
            //check disk capacity //compress => warn for low disk //decompress => fail low disk
            Console.ReadLine();
        }

        private static void Compress(CompressOptions options)
        {
            var settings = new CompressSettings { ChunkSize = ConvertMegabytesToBytes(options), PoolSize = 4 };
            IStrategy strategy = new CompressStrategy(settings);
            Work(strategy, options);
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
            Work(strategy, options);
        }

        private static void Work(IStrategy strategy, Options options)
        {
            Logger.SetWriter(new ConsoleWriter(Level.Info));
            try
            {
                var stopwatch = new Stopwatch();
                Logger.Log(Level.Info, $"starting {strategy.Name}");
                stopwatch.Start();
                strategy.Work(options.Source, options.Destination);
                stopwatch.Stop();
                Logger.Log(Level.Info, $"{strategy.Name} took {stopwatch.Elapsed}");
            }
            catch(Exception ex)
            {
                Logger.Log(Level.Fatal, $"error occurred while executing {strategy.Name}", ex);
            }
        }
    }
}
