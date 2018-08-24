using CommandLine;
using packer;
using System;
using System.Collections.Generic;

namespace console
{
    class Program
    {
        public class Options
        {
            [Option('s', "source", Required = true, HelpText = "Path to source file")]
            public string Source { get; set; }

            [Option('d', "destination", Required = true, HelpText = "Path to destination file")]
            public string Destination { get; set; }

            [Option('c', "chunksize", Required = false, Default = 1, HelpText = "Size of chunk in megabytes that will be used for compress")]
            public int Chunksize { get; set; }

            [Option('p', "poolsize", Required = false, Default = 4, HelpText = "Threads amount used for operations")]
            public int Poolsize { get; set; }

            [Option('o', "operation", Required = true, Default = Operations.Compress, HelpText = "Threads amount used for operations")]
            public Operations Operation { get; set; }

            public enum Operations
            {
                Compress,
                Decompres
            }
        }

        //fsutil file createnew C:\testfile.txt 1000
        static void Main(string[] args)
        {
            ParserResult<Options> result = Parser.Default.ParseArguments<Options>(args);
            result.WithParsed(Work);
            Console.ReadLine();
        }

        private static void Work(Options options)
        {
            switch (options.Operation)
            {
                case Options.Operations.Compress: Compress(options); break;
                case Options.Operations.Decompres: Decompress(options); break;
            }
        }

        private static void Compress(Options options)
        {
            var settings = new CompressSettings { ChunkSize = options.Chunksize * 1024 * 1024, PoolSize = options.Poolsize };
            IStrategy strategy = new CompressStrategy(settings);
            strategy.Work(options.Source, options.Destination);
        }

        private static void Decompress(Options options)
        {
            IStrategy strategy = new DecompressStrategy(options.Poolsize);
            strategy.Work(options.Source, options.Destination);
        }
    }
}
