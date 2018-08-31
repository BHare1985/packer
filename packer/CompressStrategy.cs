using SimpleLogger;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using ThreadPool;

namespace packer
{
    public class CompressStrategy : IStrategy
    {
        private readonly CompressFactory _factory;
        private readonly CompressSettings _settings;

        public string Name => "Compression";

        internal CompressStrategy(CompressFactory factory, CompressSettings settings)
        {
            _factory = factory;
            _settings = settings;
        }

        public CompressStrategy(CompressSettings settings) : this(new CompressFactory(settings.PoolSize), settings) { }

        public void Work(string source, string destination)
        {
            var sourceInfo = new FileInfo(source);
            var chunkCount = (int)Math.Ceiling(sourceInfo.Length / (decimal)_settings.ChunkSize);

            Logger.Log(Level.Verbose, $"file {source} with size {sourceInfo.Length} bytes will be processed in {chunkCount} chunks");

            var metadata = _factory.GetMetadataWriter(destination);
            using (var pool = _factory.GetThreadPool())
            {
                using (var manager = _factory.GetFileManager(destination, _settings.ChunkSize))
                {
                    var reader = _factory.GetByteReader(source);
                    var writer = _factory.GetByteWriter(manager);
                    var compressor = _factory.GetCompressor();
                    var readyCount = 0;
                    for (var i = 0; i < chunkCount; i++)
                    {
                        var index = i;
                        Logger.Log(Level.Verbose, $"queuing work for chunk number {index}");
                        pool.Queue(QueueType.Read, () => reader.Read(index, _settings.ChunkSize * (long)index, _settings.ChunkSize))
                            .Then(QueueType.Zip, _ => compressor.Zip(_.Result, index))
                            .Then(QueueType.Write, _ => writer.Write(_.Result, index))
                            .Then(QueueType.Write, _ => { metadata.Add(_.Result); Logger.Log(Level.Info, $"chunks {Interlocked.Increment(ref readyCount)} of {chunkCount} ready"); });
                    }
                    Logger.Log(Level.Verbose, $"waiting for queued work to be ready");
                    pool.Wait();
                }
            }
            metadata.Write(_settings.ChunkSize, sourceInfo.Length);
            Logger.Log(Level.Verbose, "preparing to writer compressing metadata");
        }

        private static void LogProgress(Chunk chunk)
        {
            var message = $"chunks {0} of {chunk.Offset} ready";
        }
    }
}
