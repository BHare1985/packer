using SimpleLogger;
using System.Linq;
using System.Threading;
using ThreadPool;

namespace packer
{
    public class DecompressStrategy : IStrategy
    {
        private readonly DecompressFactory _factory;

        public DecompressStrategy(int poolSize) : this(new DecompressFactory(poolSize)) { }

        internal DecompressStrategy(DecompressFactory factory)
        {
            _factory = factory;
        }

        public string Name => "Decompression";

        public void Work(string source, string destination)
        {
            Logger.Log(Level.Debug, $"reading metadata from {source}");
            var metaReader = _factory.GetMetadataReader(source);
            Metadata metadata = metaReader.Read();
            Logger.Log(Level.Debug, $"metadata ready starting decompression");

            using (var pool = _factory.GetThreadPool())
            {
                using (var manager = _factory.GetFileManager(destination, metadata.Length))
                {
                    var reader = _factory.GetByteReader(source);
                    var writer = _factory.GetByteWriter(manager);
                    var decompressor = _factory.GetDecompressor();

                    var readyCount = 0;
                    foreach (var chunk in metadata.Chunks.OrderBy(_ => _.Offset))
                    {
                        pool.Queue(QueueType.Read, () => reader.Read(chunk.Index, chunk.Offset, chunk.Size))
                            .Then(QueueType.Zip, _ => decompressor.Zip(_.Result, chunk.Index))
                            .Then(QueueType.Write, _ => writer.Write(_.Result, chunk.Index, chunk.Index * metadata.ChunkSize))
                            .Then(QueueType.Write, () => Logger.Log(Level.Info, $"chunks {Interlocked.Increment(ref readyCount)} of {metadata.Chunks.Length} ready"));
                    }
                    Logger.Log(Level.Verbose, $"waiting for {metadata.Chunks.Length} tasks to be processed");
                    SpinWait.SpinUntil(() => readyCount == metadata.Chunks.Length);
                    Logger.Log(Level.Verbose, "all queued tasks were processed");
                }
            }
        }
    }
}
