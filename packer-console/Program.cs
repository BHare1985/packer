using CommandLine;
using packer;
using System;
using SimpleLogger;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace console
{
    partial class Program
    {
        //fsutil file createnew C:\testfile.txt 1000
        static void Main(params string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            try
            {
                var result = Parser.Default.ParseArguments<CompressOptions, DecompressOptions>(args);
                result.WithParsed<DecompressOptions>(Decompress).WithParsed<CompressOptions>(Compress);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            
            Console.WriteLine("FINISHED. Press any key");
            Console.ReadKey();
        }

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("Unhandled exception accrued in {0}: {1}", sender, e.ExceptionObject);
            Logger.Log(Level.Fatal, $"unhandled exception occurred in {sender}", (Exception)e.ExceptionObject);
        }

        private static void Compress(CompressOptions options)
        {
            var settings = new CompressSettings { ChunkSize = ConvertMegabytesToBytes(options), PoolSize = options.Poolsize };
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
            Logger.SetWriter(new ConsoleWriter(options.LogLevel));

            CheckReadWrite(options);

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
                Logger.Log(Level.Fatal, $"error occurred while executing {strategy.Name}. Can not work further. Terminating", ex);
            }
        }

        private static void CheckReadWrite(Options options)
        {
            //check read permissions for source file
            //check destination file dos not exists //rewrite?
            //check write permissions
            //check disk capacity //compress => warn for low disk //decompress => fail low disk

            try
            {
                var source = new FileInfo(options.Source);
                if (source.Length == 0) throw new SourceFileIsEmptyException($"nothing to compress so as file {options.Source} is empty");
                using (var file = source.OpenRead())
                {
                    var buffer = new byte[1];
                    file.Read(buffer, 0, buffer.Length);
                }
            }
            catch(Exception ex)
            {
                throw new ProblemWithSourceFileException($"can not read {options.Source} file. please, try to check permissions path or file", ex);
            }

            try
            {
                var destination = new FileInfo(options.Destination);
                if (destination.Exists && !options.IsForce) throw new FileAlreadyExistsException($"destination file {options.Destination} already exists. use force option to overwrite");
                var test = "TEST STRING";
                using (var file = destination.Open(FileMode.OpenOrCreate))
                {
                    var payload = Encoding.UTF8.GetBytes(test);
                    file.Write(payload, 0, payload.Length);
                    file.Flush();
                }
                using (var file = destination.Open(FileMode.Open))
                {
                    var buffer = new byte[test.Length];
                    var count = file.Read(buffer, 0, buffer.Length);
                }
                File.Delete(options.Destination);
            }
            catch (Exception ex)
            {
                throw new ProblemWithDestinationFileException($"can not read or write to destination {options.Destination} file. please check permissions path or file", ex);
            }
        }
    }

    public class FileAlreadyExistsException : Exception
    {
        public FileAlreadyExistsException(string message) : base(message)
        {
        }
    }

    public class ProblemWithDestinationFileException : Exception
    {
        public ProblemWithDestinationFileException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class SourceFileIsEmptyException : Exception
    {
        public SourceFileIsEmptyException(string message) : base(message)
        {
        }
    }

    public class ProblemWithSourceFileException : Exception
    {
        public ProblemWithSourceFileException(string message) : base(message)
        {
        }

        public ProblemWithSourceFileException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

}
