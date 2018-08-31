using SimpleLogger;

namespace packer
{
    public class CompressFactory
    {
        private readonly int _poolSize;

        public CompressFactory(int poolSize)
        {
            _poolSize = poolSize;
        }

        public ThreadPool.ThreadPool GetThreadPool()
        {
            Logger.Log(Level.Debug, $"initialize thread pool with {_poolSize} threads");
            return new ThreadPool.ThreadPool(_poolSize);
        }

        internal CompressFileManager GetFileManager(string file, long chunkSize)
        {
            Logger.Log(Level.Debug, $"initialize file manager");
            return new CompressFileManager(file, _poolSize, chunkSize);
        }

        internal ByteReader GetByteReader(string file)
        {
            Logger.Log(Level.Debug, $"initialize byte reader");
            return new ByteReader(file);
        }

        internal Compressor GetCompressor()
        {
            Logger.Log(Level.Debug, $"initialize compressor");
            return new Compressor();
        }

        internal ByteWriter GetByteWriter(CompressFileManager manager)
        {
            Logger.Log(Level.Debug, $"initialize byte writer");
            return new ByteWriter(manager);
        }

        internal MetadataWriter GetMetadataWriter(string file)
        {
            Logger.Log(Level.Debug, $"initialize metadata writer");
            return new MetadataWriter(file);
        }
    }
}
