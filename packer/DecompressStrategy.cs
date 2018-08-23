using System.Linq;

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

        public void Work(string source, string destination)
        {
            var metaReader = new MetadataReader(source);
            Metadata metadata = metaReader.Read();

            using (var pool = _factory.GetThreadPool())
            {
                using (var manager = _factory.GetFileManager(destination, metadata.Length))
                {
                    var reader = _factory.GetByteReader(source);
                    var writer = _factory.GetByteWriter(manager);
                    var decompressor = _factory.GetDecompressor();

                    for (var i = 0; i < metadata.Chunks.Length; i++)
                    {
                        var chunk = metadata.Chunks[i];
                        var read = pool.Queue(ThreadPool.QueueType.Read, () => reader.Read(chunk.Index, chunk.Offset, chunk.Size));
                        var zip = read.Then(ThreadPool.QueueType.Zip, () => decompressor.Zip(read.Result, chunk.Index));
                        zip.Then(ThreadPool.QueueType.Write, () => writer.Write(zip.Result, chunk.Index, chunk.Index * metadata.ChunkSize));
                    }
                    pool.Wait();
                }
            }
        }
    }
}
