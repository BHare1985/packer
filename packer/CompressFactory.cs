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
            return new ThreadPool.ThreadPool(_poolSize);
        }

        internal FileManager GetFileManager(string file)
        {
            return new FileManager(file, _poolSize);
        }

        internal ByteReader GetByteReader(string file)
        {
            return new ByteReader(file);
        }

        internal Compressor GetCompressor()
        {
            return new Compressor();
        }

        internal ByteWriter GetByteWriter(FileManager manager)
        {
            return new ByteWriter(manager);
        }

        internal MetadataWriter GetMetadataWriter(string file)
        {
            return new MetadataWriter(file);
        }
    }
}
